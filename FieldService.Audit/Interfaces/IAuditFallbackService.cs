using FieldService.Audit.Entities;
using FiledService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditFallbackService
{
    Task SaveFallbackSessionActivitiesAsync(CancellationToken ct = default);

    Task SaveFallbackAuditChangesAsync(CancellationToken ct = default);

    Task SaveFallbackAuditAccessesAsync(CancellationToken ct = default);

    Task AddSessionActivityFallbackAsync(
        AuditRequest auditRequest,
        CancellationToken ct = default);

    Task AddAuditChangeFallbackAsync(
        AuditChange auditChange,
        CancellationToken ct = default);

    Task AddAuditAccessFallbackAsync(
        AuditAccess auditAccess,
        CancellationToken ct = default);
}

