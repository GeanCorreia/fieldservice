using FiledService.Audit.Entities;

namespace FiledService.Audit.Interfaces;

public interface IAuditChangeRepository
{
    Task Save(AuditChange change);
    Task Save(IEnumerable<AuditChange> changes);
    
    Task Save(AuditAccess access);
}