using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;

namespace FiledService.Audit.Services;

public class AuditChangeFallbackService(IAuditFallbackService fallbackService) : IAuditChangeFallbackService
{
    public async Task AddAuditChangeFallbackAsync(
        AuditChange auditChange,
        CancellationToken ct = default)
    {
        await fallbackService.AddAuditChangeFallbackAsync(auditChange, ct);
    }
}

