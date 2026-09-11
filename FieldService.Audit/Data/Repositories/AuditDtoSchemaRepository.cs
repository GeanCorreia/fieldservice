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

        await dbContext.Set<AuditDtoSchema>().AddRangeAsync(items, ct);
        _ = await dbContext.SaveChangesAsync(ct);
    }
}
