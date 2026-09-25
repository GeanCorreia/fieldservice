using FieldService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditRequestRepository
{
    Task SaveAsync(IEnumerable<AuditRequest> activities, CancellationToken ct = default);
    Task SaveAsync(AuditRequest activity, CancellationToken ct = default);
}