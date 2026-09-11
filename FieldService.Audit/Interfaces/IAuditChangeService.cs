namespace FiledService.Audit.Interfaces;

public interface IAuditChangeService
{
    Task AuditChanges(CancellationToken ct = default);
}
