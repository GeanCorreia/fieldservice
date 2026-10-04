using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetContainerRepository
{
    Task<SupersetContainer?> GetSupersetContainerByTenantIdAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
    Task<SupersetContainer?> GetSupersetContainerByIdAsync(
        Guid containerId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<SupersetContainer>> GetSupersetContainersAsync();
    
    Task SaveSupersetContainerAsync(
        SupersetContainer supersetContainer, 
        CancellationToken cancellationToken = default);
}