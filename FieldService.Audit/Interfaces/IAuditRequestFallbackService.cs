using FieldService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditRequestFallbackService
{
    Task SaveFallbackSessionActivitiesAsync(
        CancellationToken ct = default);
    Task AddSessionActivityFallbackAsync(
        AuditRequest auditRequest,
        CancellationToken ct = default);
}