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
    
}