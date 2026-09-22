namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantInstanceProcessingLock
{
    Task<bool> AcquireLock(Guid tenantId, CancellationToken ct = default);
    Task<bool> ReleaseLock(Guid tenantId, CancellationToken ct = default);
}