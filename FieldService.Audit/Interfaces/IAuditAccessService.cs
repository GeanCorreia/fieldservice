using FieldService.Shared.Types;
using FiledService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditAccessService
{
    Task AuditAccess(
        string resourceName,
        SchemaVersion schemaVersion,
        Guid? resourceId,
        IEnumerable<AuditAccessParameter>? parameters,
        CancellationToken ct = default);
}
