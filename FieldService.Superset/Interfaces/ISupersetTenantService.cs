using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantService : ISupersetRepository
{
    Task SaveSupersetTenantInstanceAsync(
        SupersetTenantInstance supersetTenantInstance, 
        CancellationToken cancellationToken = default);
    Task<SupersetTenantInstance?> GetSupersetTenantInstance(
        Guid tenantId, 
        SupersetContainerInstanceStatus? status = SupersetContainerInstanceStatus.Running,
        CancellationToken cancellationToken = default);
    
    Task EnsureSupersetContainerActiveAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
}