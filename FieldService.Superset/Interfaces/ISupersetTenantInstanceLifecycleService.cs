using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantInstanceLifecycleService
{
    Task ScaleUpContainerAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
    Task ScaleDownToZeroAsync(
        Guid tenantId,  
        CancellationToken cancellationToken = default);
    
    Task<SupersetHealthCheck> HealthCheckAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
    Task<bool> IsContainerActiveAsync(
        string azureResourceId, 
        CancellationToken cancellationToken = default);

    Task<SupersetTenantInstance?> GetSupersetTenantInstance(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    
}