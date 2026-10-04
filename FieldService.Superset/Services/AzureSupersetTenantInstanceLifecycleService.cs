using System.Diagnostics;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
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
    private readonly ArmClient _armClient;

    public AzureSupersetTenantInstanceLifecycleService(
        ILogger<AzureSupersetTenantInstanceLifecycleService> logger,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ISupersetApi supersetApi,
        ISupersetRepository supersetRepository,
        HybridCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetRepository = supersetRepository ?? throw new ArgumentNullException(nameof(supersetRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _armClient = new ArmClient(new DefaultAzureCredential());
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
            var resourceId = new ResourceIdentifier(azureResourceId);
            var containerAppResource = _armClient.GetContainerAppResource(resourceId);
            
            var response = await containerAppResource.GetAsync(cancellationToken);
            var containerApp = response.Value;

            var scaleTarget = ResolveScaleTarget(resource.Container.ExecutionType, isScaleUp: true);
            containerApp.Data.Template.Scale.MinReplicas = scaleTarget.MinReplicas;
            containerApp.Data.Template.Scale.MaxReplicas = scaleTarget.MaxReplicas;

            await containerAppResource.UpdateAsync(WaitUntil.Completed, containerApp.Data, cancellationToken);
            await EnsureContainerHealthyAsync(resource.Container.FqdnUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogSupersetTenantInstanceScaleUpContainerError(LogLevel.Error, tenantId, ex.Message, ex);
            throw;
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
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
            var resourceId = new ResourceIdentifier(azureResourceId);
            var containerAppResource = _armClient.GetContainerAppResource(resourceId);
            
            var response = await containerAppResource.GetAsync(cancellationToken);
            var containerApp = response.Value;

            var scaleTarget = ResolveScaleTarget(resource.Container.ExecutionType, isScaleUp: false);
            containerApp.Data.Template.Scale.MinReplicas = scaleTarget.MinReplicas;
            containerApp.Data.Template.Scale.MaxReplicas = scaleTarget.MaxReplicas;

            await containerAppResource.UpdateAsync(WaitUntil.Completed, containerApp.Data, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to scale down container for tenant {tenantId}: {ex.Message}", ex);
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
        }
    }

    public async Task<bool> IsContainerActiveAsync(
        string azureResourceId, 
        CancellationToken cancellationToken = default)
    {
        var containerApp = await GetContainerAppAsync(azureResourceId, cancellationToken);
        var status = MapSupersetInstanceStatus(containerApp.Data);

        return status == SupersetContainerInstanceStatus.Running;
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
        catch (Exception ex)
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
            catch
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
            var containerApp = await GetContainerAppAsync(tenantConfig.Container.ResourceId, cancellationToken);
            var status = MapSupersetInstanceStatus(containerApp.Data);

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

    private async Task<ContainerAppResource> GetContainerAppAsync(
        string azureResourceId,
        CancellationToken cancellationToken)
    {
        var resourceId = new ResourceIdentifier(azureResourceId);
        var containerAppResource = _armClient.GetContainerAppResource(resourceId);

        var response = await containerAppResource.GetAsync(cancellationToken);
        return response.Value;
    }

    private static SupersetContainerInstanceStatus MapSupersetInstanceStatus(ContainerAppData containerAppData)
    {
        ArgumentNullException.ThrowIfNull(containerAppData);

        var minReplicas = containerAppData.Template?.Scale?.MinReplicas ?? 0;
        var provisioningState = NormalizeProvisioningState(containerAppData.ProvisioningState.ToString() ?? string.Empty);

        return provisioningState switch
        {
            "failed" => SupersetContainerInstanceStatus.Failed,
            "succeeded" => minReplicas > 0
                ? SupersetContainerInstanceStatus.Running
                : SupersetContainerInstanceStatus.Stopped,
            "provisioning" => SupersetContainerInstanceStatus.Provisioning,
            "inprogress" => SupersetContainerInstanceStatus.Provisioning,
            "pending" => SupersetContainerInstanceStatus.Provisioning,
            "accepted" => SupersetContainerInstanceStatus.Provisioning,
            "creating" => SupersetContainerInstanceStatus.Provisioning,
            "updating" => SupersetContainerInstanceStatus.Provisioning,
            "waiting" => SupersetContainerInstanceStatus.Provisioning,
            "deleting" => SupersetContainerInstanceStatus.Deleting,
            "canceled" => SupersetContainerInstanceStatus.Canceled,
            _ => minReplicas > 0
                ? SupersetContainerInstanceStatus.Provisioning
                : SupersetContainerInstanceStatus.Unknown
        };
    }

    private static string NormalizeProvisioningState(string provisioningState)
    {
        if (string.IsNullOrWhiteSpace(provisioningState))
        {
            return string.Empty;
        }

        return provisioningState
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static (int MinReplicas, int MaxReplicas) ResolveScaleTarget(
        ExecutionType executionType,
        bool isScaleUp)
    {
        if (isScaleUp)
        {
            return executionType switch
            {
                ExecutionType.OnDemand => (0, 1),
                ExecutionType.Scheduled => (0, 1),
                _ => (1, 1)
            };
        }

        return executionType switch
        {
           
            ExecutionType.OnDemand => (0, 1),
            ExecutionType.Scheduled => (0, 1),
            ExecutionType.AlwaysOn => (1, 1),
            _ => (0, 1)
        };
    }
}