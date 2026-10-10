namespace FieldService.Form.Interfaces;

public interface IFormProcessingLock
{
    Task<bool> AcquireLock(Guid tenantId, CancellationToken ct = default);
    Task<bool> ReleaseLock(Guid tenantId, CancellationToken ct = default);
}