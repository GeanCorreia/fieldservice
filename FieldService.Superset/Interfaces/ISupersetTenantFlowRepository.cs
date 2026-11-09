using FieldService.Superset.Entities;
using FieldService.Shared.Enums;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantFlowRepository
{
    Task<SupersetTenantFlow?> GetByIdAsync(
        Guid creationId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetTenantFlow>> GetByStatusAsync(
        FlowStatus status, 
        CancellationToken cancellationToken);
    
    Task SaveAsync(
        SupersetTenantFlow supersetTenantFlow, 
        CancellationToken cancellationToken);
}