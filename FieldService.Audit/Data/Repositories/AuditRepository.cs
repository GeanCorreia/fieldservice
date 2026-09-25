using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;

namespace FiledService.Audit.Data.Repositories;

internal sealed class AuditRepository(AuditDbContext dbContext) : IAuditChangeRepository
{
    public async Task Save(AuditChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        await dbContext.Changes.AddAsync(change);
        _ = await dbContext.SaveChangesAsync();
    }

    public async Task Save(IEnumerable<AuditChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var items = changes.ToList();
        if (items.Count == 0)
            return;

        await dbContext.Changes.AddRangeAsync(items);
        _ = await dbContext.SaveChangesAsync();
    }

    public async Task Save(AuditAccess access)
    {
        ArgumentNullException.ThrowIfNull(access);

        await dbContext.Accesses.AddAsync(access);
        _ = await dbContext.SaveChangesAsync();
    }
}
