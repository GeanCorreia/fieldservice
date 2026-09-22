using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetRepository
{
    Task<SupersetTenantConfig?> GetSupersetTenantByTenantIdAsync(
        Guid tenantId, 
        CancellationToken cancellationToken);
    
    Task<SupersetTenantConfig?> GetSupersetTenantByResourceIdAsync(
        string resourceId, 
        CancellationToken cancellationToken);
    
    Task<SupersetTenantConfig> SaveAsync(
        SupersetTenantConfig tenantConfig, 
        CancellationToken cancellationToken);
}