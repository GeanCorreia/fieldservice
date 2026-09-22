using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Logs;
using Microsoft.Extensions.Logging;
using Refit;

namespace FieldService.Superset.Services;

internal class SupersetTenantInstanceLifecycleService : ISupersetTenantInstanceLifecycleService
{
    private readonly ILogger<SupersetTenantInstanceLifecycleService> _logger;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetService _supersetService;
    private readonly ArmClient _armClient;

    public SupersetTenantInstanceLifecycleService(
        ILogger<SupersetTenantInstanceLifecycleService> logger,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ISupersetApi supersetApi,
        ISupersetService supersetService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _armClient = new ArmClient(new DefaultAzureCredential());
    }
    
    public async Task ScaleUpContainerAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
       
        
        
        var resource = await _supersetService.GetSupersetTenantByTenantIdAsync(tenantId, cancellationToken: cancellationToken);
        if (resource == null)
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
            var exception = new SupersetTenantNotFoundException(tenantId);
            
            _logger.LogSupersetTenantInstanceScaleUpContainerError(
                LogLevel.Error, 
                tenantId, 
                exception.Message, 
                exception);
            
            throw exception;
        }
        var azureResourceId = resource.ResourceId;
        
        if(IsContainerActiveAsync(azureResourceId, cancellationToken).Result)
        {
            return;
        }
        
        if(await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot scale up container for tenant {tenantId} because another operation is in progress.");
        }
        

        try
        {
            var resourceId = new ResourceIdentifier(azureResourceId);
            var containerAppResource = _armClient.GetContainerAppResource(resourceId);
            var containerApp = containerAppResource.Get();

            containerApp.Value.Data.Template.Scale.MinReplicas = 1;
            containerApp.Value.Data.Template.Scale.MaxReplicas = 1;

            await containerAppResource.UpdateAsync(Azure.WaitUntil.Completed, containerApp.Value.Data,
                cancellationToken);
            
        }
        catch (Exception ex)
        {
            _logger.LogSupersetTenantInstanceScaleUpContainerError(
                LogLevel.Error, 
                tenantId, 
                ex.Message, 
                ex);

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

        var resource = await _supersetService.GetSupersetTenantByTenantIdAsync(tenantId, cancellationToken: cancellationToken);
        if (resource == null)
        {
            
            var exception = new SupersetTenantNotFoundException(tenantId);
            
            _logger.LogSupersetTenantInstanceScaleUpContainerError(
                LogLevel.Error, 
                tenantId, 
                exception.Message, 
                exception);
            
            throw exception;
        }
        
       

        var azureResourceId = resource.ResourceId;
        if(!await IsContainerActiveAsync(azureResourceId, cancellationToken))
        {
            return;
        }
        

        if(await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot scale up container for tenant {tenantId} because another operation is in progress.");
        }

        
        try
        {
            var resourceId = new ResourceIdentifier(azureResourceId);
            var containerAppResource = _armClient.GetContainerAppResource(resourceId);
            var containerApp = containerAppResource.Get();
            containerApp.Value.Data.Template.Scale.MinReplicas = 0;
            containerApp.Value.Data.Template.Scale.MaxReplicas = 1;
            
            await containerAppResource.UpdateAsync(Azure.WaitUntil.Completed, containerApp.Value.Data, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to scale down container for tenant {tenantId}: {ex.Message}", ex);
        }
    }
    
    public async Task<bool> IsContainerActiveAsync(
        string azureResourceId, 
        CancellationToken cancellationToken = default)
    {
        var resourceId = new ResourceIdentifier(azureResourceId);
        var containerAppResource = _armClient.GetContainerAppResource(resourceId);
        
        var response = await containerAppResource.GetAsync(cancellationToken);
        var containerApp = response.Value;
        
        var isRunning = containerApp.Data.ProvisioningState == ContainerAppProvisioningState.Succeeded;
        return isRunning && containerApp.Data.Template.Scale.MinReplicas > 0;
    }

    public async Task<SupersetHealthCheck> HealthCheckAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var instance = await _supersetService.GetSupersetTenantInstance(tenantId, cancellationToken: cancellationToken);
        if (instance == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }

        var fqdnUrl = instance.TenantConfig.FqdnUrl;
        var azureResourceId = instance.TenantConfig.ResourceId ?? string.Empty; 

        bool isHealthy = false;
        int httpStatusCode = 500;
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
}