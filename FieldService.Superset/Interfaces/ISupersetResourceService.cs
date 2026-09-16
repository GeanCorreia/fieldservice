using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetResourceService
{
    Task<SupersetResource?> GetResourceBySupersetIdAsync(string supersetId, CancellationToken ct);
}