using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantFlowService
{
    Task<SupersetTenant> BuildSupersetTenantCreationFlowAsync(
        Guid flowId,
        CancellationToken cancellationToken = default);
    
    Task<SupersetTenant> BuildSupersetTenantUpdateFlowAsync(
        Guid flowId,
        CancellationToken cancellationToken = default);
    
}