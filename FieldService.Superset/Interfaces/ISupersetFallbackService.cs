using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetFallbackService
{
    Task<IEnumerable<SupersetTenantConfig>> GetAllFallbacksAsync(CancellationToken cancellationToken);
    Task AddFallbackAsync(SupersetTenantConfig fallbackConfig, CancellationToken cancellationToken);
    Task RemoveFallbackAsync(Guid fallbackConfigId, CancellationToken cancellationToken);
    Task RemoveFallbackAsync(IEnumerable<Guid> fallbackConfigIds, CancellationToken cancellationToken);
}