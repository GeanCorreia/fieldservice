using FiledService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditDtoSchemaRepository
{
    Task<IReadOnlyCollection<AuditDtoSchema>> GetAll(CancellationToken ct = default);
    Task Save(IEnumerable<AuditDtoSchema> schemas, CancellationToken ct = default);
}
