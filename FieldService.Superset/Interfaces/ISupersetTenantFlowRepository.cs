using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantFlowRepository
{
    Task<SupersetContainerDeploymentFlow?> GetByIdAsync(
        Guid creationId, 
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetContainerDeploymentFlow>> GetByStatusAsync(
        SupersetTenantDeployStatus status, 
        CancellationToken cancellationToken);
    
    Task SaveAsync(
        SupersetContainerDeploymentFlow supersetContainerDeploymentFlow, 
        CancellationToken cancellationToken);
}