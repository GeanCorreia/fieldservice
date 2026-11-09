using FieldService.Cloud.Interfaces;
using FieldService.Superset.Configuration;
using FieldService.Superset.Interfaces;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Services;

internal class SupersetTenantInstanceProcessingLock(
    IContainerProcessingLock processingLock,
    IOptions<SupersetOptions> supersetOptions) : ISupersetTenantInstanceProcessingLock
{
    private readonly IContainerProcessingLock _processingLock =
        processingLock ?? throw new ArgumentNullException(nameof(processingLock));
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(Math.Max(1,
        (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value.LockTtlMinutes));

    public Task<bool> AcquireLock(Guid tenantId, CancellationToken ct = default) =>
        _processingLock.AcquireAsync(GetLockKey(tenantId), _timeout, ct);

    public Task<bool> ReleaseLock(Guid tenantId, CancellationToken ct = default) =>
        _processingLock.ReleaseAsync(GetLockKey(tenantId), ct);

    private static string GetLockKey(Guid tenantId) => $"SupersetTenantInstance:{tenantId}";
}
