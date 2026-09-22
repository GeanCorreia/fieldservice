using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetService : ISupersetRepository
{
    Task SaveSupersetTenantInstanceAsync(
        SupersetTenantInstance supersetTenantInstance, 
        CancellationToken cancellationToken = default);
    Task<SupersetTenantInstance?> GetSupersetTenantInstance(
        Guid tenantId, 
        SupersetInstanceStatus? status = SupersetInstanceStatus.Running,
        CancellationToken cancellationToken = default);
    
    Task<bool> HasSuperset(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<string?> GetAdminToken(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
    Task<string> GetAdminToken(
        string fqdnUrl, 
        CancellationToken cancellationToken = default);
    
    Task<SupersetTenantResources> GetTenantResourcesAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
    
   
}