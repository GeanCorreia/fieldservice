using System.Diagnostics;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;
using Azure;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Logs;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Refit;

namespace FieldService.Superset.Services;

internal class AzureSupersetTenantInstanceLifecycleService : ISupersetTenantInstanceLifecycleService
{
    private readonly ILogger<AzureSupersetTenantInstanceLifecycleService> _logger;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetRepository _supersetRepository;
    private readonly HybridCache _cache;
    private readonly IAzureContainerService _containers;

    public AzureSupersetTenantInstanceLifecycleService(
        ILogger<AzureSupersetTenantInstanceLifecycleService> logger,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ISupersetApi supersetApi,
        ISupersetRepository supersetRepository,
        HybridCache cache,
        IAzureContainerService containers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetRepository = supersetRepository ?? throw new ArgumentNullException(nameof(supersetRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _containers = containers ?? throw new ArgumentNullException(nameof(containers));
    }

    public async Task ScaleUpContainerAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetTenantConfigByTenantIdAsync(tenantId, cancellationToken);
        if (resource == null)
        {
            var exception = new SupersetTenantNotFoundException(tenantId);
            _logger.LogSupersetTenantInstanceScaleUpContainerError(LogLevel.Error, tenantId, exception.Message, exception);
            throw exception;
        }

        var azureResourceId = resource.Container.ResourceId;

        if (await IsContainerActiveAsync(azureResourceId, cancellationToken))
        {
            return;
        }

        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Cannot scale up container for tenant {tenantId} because another operation is in progress.");
        }

        try
        {
            await _containers.ApplyExecutionScaleAsync(
                azureResourceId, resource.Container.ExecutionType, isScaleUp: true, cancellationToken);
            await EnsureContainerHealthyAsync(resource.Container.FqdnUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogSupersetTenantInstanceScaleUpContainerError(LogLevel.Error, tenantId, ex.Message, ex);
            throw;
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, CancellationToken.None);
        }
    }

    public async Task ScaleDownToZeroAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetTenantConfigByTenantIdAsync(tenantId, cancellationToken);
        if (resource == null)
        {
            var exception = new SupersetTenantNotFoundException(tenantId);
            _logger.LogSupersetTenantInstanceScaleUpContainerError(LogLevel.Error, tenantId, exception.Message, exception);
            throw exception;
        }

        var azureResourceId = resource.Container.ResourceId;

        if (!await IsContainerActiveAsync(azureResourceId, cancellationToken))
        {
            return;
        }

        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Cannot scale down container for tenant {tenantId} because another operation is in progress.");
        }

        try
        {
            await _containers.ApplyExecutionScaleAsync(
                azureResourceId, resource.Container.ExecutionType, isScaleUp: false, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to scale down container for tenant {tenantId}: {ex.Message}", ex);
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, CancellationToken.None);
        }
    }

    public async Task<bool> IsContainerActiveAsync(
        string azureResourceId,
        CancellationToken cancellationToken = default)
    {
        return await _containers.GetStatusAsync(azureResourceId, cancellationToken) == ContainerInstanceStatus.Running;
    }

    public async Task<SupersetHealthCheck> HealthCheckAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var instance = await GetSupersetTenantInstance(tenantId, cancellationToken);
        if (instance == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }

        var fqdnUrl = instance.Tenant.Container.FqdnUrl;
        var azureResourceId = instance.Tenant.Container.ResourceId;

        bool isHealthy;
        int httpStatusCode;
        string? errorMessage = null;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _supersetApi.GetHealthAsync(new Uri(fqdnUrl), cancellationToken);
            isHealthy = true;
            httpStatusCode = 200;
        }
        catch (ApiException apiEx)
        {
            isHealthy = false;
            httpStatusCode = (int)apiEx.StatusCode;
            errorMessage = apiEx.Content ?? apiEx.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            isHealthy = false;
            httpStatusCode = 503;
            errorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
        }

        return SupersetHealthCheck.Create(
            azureResourceId,
            tenantId,
            isHealthy,
            responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
            httpStatusCode,
            errorMessage
        );
    }

    private async Task EnsureContainerHealthyAsync(string fqdnUrl, CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;
        const int delaySeconds = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _supersetApi.GetHealthAsync(new Uri(fqdnUrl), cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt == maxAttempts) throw;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }
    }

    public async Task<SupersetTenantInstance?> GetSupersetTenantInstance(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        var tenantConfig = await GetTenantConfigByTenantIdAsync(tenantId, cancellationToken);
        if (tenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }

        try
        {
            var status = await _containers.GetStatusAsync(tenantConfig.Container.ResourceId, cancellationToken);

            return SupersetTenantInstance.Create(tenantConfig, status);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning(
                ex,
                "Superset tenant instance not found in Azure for tenant {TenantId} and resource {ResourceId}.",
                tenantId,
                tenantConfig.Container.ResourceId);

            return null;
        }
    }

    private async Task<SupersetTenant?> GetTenantConfigByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            SupersetCache.GetTenantConfigByTenantIdCacheKey(tenantId),
            async token => await _supersetRepository.GetSupersetTenantByIdAsync(tenantId, token),
            SupersetCache.ConfigCacheOptions,
            cancellationToken: cancellationToken);
    }

}