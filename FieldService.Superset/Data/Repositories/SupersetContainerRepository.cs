using FieldService.Data.Interfaces;
using FieldService.Superset.Data;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Superset.Data.Repositories;

internal sealed class SupersetContainerRepository(
    SupersetDbContext dbContext,
    ISqlUnitOfWork<SupersetDbContext> unitOfWork) : ISupersetContainerRepository
{
    public async Task<SupersetContainer?> GetSupersetContainerByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            return null;

        return await dbContext.SupersetTenants
            .AsNoTracking()
            .Include(x => x.Container)
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Container)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SupersetContainer?> GetSupersetContainerByIdAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        if (containerId == default)
            return null;

        return await dbContext.SupersetContainers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == containerId, cancellationToken);
    }

    public async Task<IEnumerable<SupersetContainer>> GetSupersetContainersAsync()
    {
        return await dbContext.SupersetContainers
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task SaveSupersetContainerAsync(
        SupersetContainer supersetContainer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supersetContainer);

        var existing = await dbContext.SupersetContainers
            .FirstOrDefaultAsync(x => x.Id == supersetContainer.Id, cancellationToken);

        if (existing is null)
        {
            await dbContext.SupersetContainers.AddAsync(supersetContainer, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(supersetContainer);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }
}

