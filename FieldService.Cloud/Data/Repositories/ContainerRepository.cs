using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;
using FieldService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Cloud.Data.Repositories;

internal sealed class ContainerRepository(
    CloudDbContext dbContext,
    ISqlUnitOfWork<CloudDbContext> unitOfWork) : IContainerRepository
{
    public Task<Container?> GetByIdAsync(Guid containerId, CancellationToken cancellationToken = default) =>
        dbContext.Containers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == containerId, cancellationToken);

    public Task<Container?> GetByTenantAndNameAsync(
        Guid tenantId,
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return dbContext.Containers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Name == resourceName, cancellationToken);
    }

    public async Task SaveAsync(Container container, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(container);
        var existing = await dbContext.Containers.SingleOrDefaultAsync(x => x.Id == container.Id, cancellationToken);
        if (existing is null)
            await dbContext.Containers.AddAsync(container, cancellationToken);
        else
            dbContext.Entry(existing).CurrentValues.SetValues(container);

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }
}