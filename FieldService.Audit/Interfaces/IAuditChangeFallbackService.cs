using FiledService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditChangeFallbackService
{
    Task AddAuditChangeFallbackAsync(
        AuditChange auditChange,
        CancellationToken ct = default);
}

