using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantFlowRepository
{
    Task<SupersetTenantFlow?> GetByIdAsync(
        Guid creationId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetTenantFlow>> GetByStatusAsync(
        SupersetTenantDeployStatus status, 
        CancellationToken cancellationToken);
    
    Task SaveAsync(
        SupersetTenantFlow supersetTenantFlow, 
        CancellationToken cancellationToken);
}