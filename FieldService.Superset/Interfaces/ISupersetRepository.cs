using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetRepository
{
    Task<SupersetTenant?> GetSupersetTenantByIdAsync(
        Guid tenantId, 
        CancellationToken cancellationToken);
    
    Task<SupersetTenant?> GetSupersetTenantByResourceIdAsync(
        string resourceId, 
        CancellationToken cancellationToken);
    
    Task SaveAsync(
        SupersetTenant tenant, 
        CancellationToken cancellationToken);
}