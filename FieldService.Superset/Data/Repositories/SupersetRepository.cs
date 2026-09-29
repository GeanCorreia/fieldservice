using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;

namespace FieldService.Superset.Data.Repositories;

internal class SupersetRepository : ISupersetRepository
{
    public async Task<SupersetTenantConfig?> GetSupersetTenantByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<SupersetTenantConfig?> GetSupersetTenantByResourceIdAsync(string resourceId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task SaveAsync(SupersetTenantConfig tenantConfig, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}