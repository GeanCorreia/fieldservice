using FiledService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditTracker
{
    Task Persist(CancellationToken ct = default);
    void TrackAccess(string resource, 
        Guid? resourceId,
        IEnumerable<AuditAccessParameter>? parameters);
}
