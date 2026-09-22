using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

public interface ISupersetResourceService
{
    Task<SupersetTenantResources> GetTenantResourcesAsync(
        string fqdnUrl,
        CancellationToken cancellationToken);
}