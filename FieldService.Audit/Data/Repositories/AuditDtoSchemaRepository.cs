using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FiledService.Audit.Data.Repositories;

internal sealed class AuditDtoSchemaRepository(AuditDbContext dbContext) : IAuditDtoSchemaRepository
{
    public async Task<IReadOnlyCollection<AuditDtoSchema>> GetAll(CancellationToken ct = default)
    {
        return await dbContext.Set<AuditDtoSchema>()
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task Save(IEnumerable<AuditDtoSchema> schemas, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        var items = schemas.ToList();
        if (items.Count == 0)
            return;

        var resourceNames = items.Select(x => x.ResourceName).Distinct().ToArray();
        var versions = items.Select(x => x.Version).Distinct().ToArray();

        var existing = await dbContext.Set<AuditDtoSchema>()
            .Where(x => resourceNames.Contains(x.ResourceName) || versions.Contains(x.Version))
            .ToListAsync(ct);

        existing = existing
            .Where(x => items.Any(i => i.ResourceName == x.ResourceName && i.Version == x.Version))
            .ToList();

        if (existing.Count > 0)
            dbContext.Set<AuditDtoSchema>().RemoveRange(existing);

        await dbContext.Set<AuditDtoSchema>().AddRangeAsync(items, ct);
        _ = await dbContext.SaveChangesAsync(ct);
    }
}
