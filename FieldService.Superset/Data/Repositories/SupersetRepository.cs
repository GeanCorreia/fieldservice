using FieldService.Data.Interfaces;
using FieldService.Superset.Data;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Superset.Data.Repositories;

internal sealed class SupersetRepository(
    SupersetDbContext dbContext,
    ISqlUnitOfWork<SupersetDbContext> unitOfWork) : ISupersetRepository
{
    public async Task<SupersetTenant?> GetSupersetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == default)
            return null;

        return await dbContext.SupersetTenants
            .AsNoTracking()
            .Include(x => x.Container)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public async Task<SupersetTenant?> GetSupersetTenantByResourceIdAsync(
        string resourceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return null;

        return await dbContext.SupersetTenants
            .AsNoTracking()
            .Include(x => x.Container)
            .FirstOrDefaultAsync(x => x.Container.ResourceId == resourceId, cancellationToken);
    }

    public async Task SaveAsync(
        SupersetTenant tenant,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (tenant.Container is not null)
        {
            var trackedContainer = await dbContext.SupersetContainers
                .FirstOrDefaultAsync(x => x.Id == tenant.Container.Id, cancellationToken);

            if (trackedContainer is null)
            {
                dbContext.Attach(tenant.Container);
            }
            else
            {
                dbContext.Entry(trackedContainer).CurrentValues.SetValues(tenant.Container);
                tenant.UpdateContainer(trackedContainer);
            }
        }

        var existing = await dbContext.SupersetTenants
            .FirstOrDefaultAsync(x => x.Id == tenant.Id || x.TenantId == tenant.TenantId, cancellationToken);

        if (existing is null)
        {
            await dbContext.SupersetTenants.AddAsync(tenant, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(tenant);
            existing.UpdateContainer(tenant.Container);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }
}