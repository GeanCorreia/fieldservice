using System.Collections.Concurrent;
using FieldService.Superset.Configuration;
using FieldService.Superset.Interfaces;
using Medallion.Threading;
using Microsoft.Extensions.Configuration;

namespace FieldService.Superset.Services;

internal class SupersetTenantInstanceProcessingLock : ISupersetTenantInstanceProcessingLock
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ConcurrentDictionary<Guid, IDistributedSynchronizationHandle> _instanceLocks = new();
    private readonly TimeSpan _lockTtl;
    private readonly TimeSpan _releaseTimeout;
    
    public SupersetTenantInstanceProcessingLock(
        IDistributedLockProvider lockProvider,
        IConfiguration configuration)
    {
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var supersetOptions = configuration.GetSection("Superset").Get<SupersetOptions>()
            ?? new SupersetOptions();

        _lockTtl = TimeSpan.FromMinutes(Math.Max(1, supersetOptions.LockTtlMinutes));
        _releaseTimeout = _lockTtl;
    }
    
    public async Task<bool> AcquireLock(Guid tenantId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await TryAcquireAsync(tenantId, ct, storeHandle: true);
    }

    public async Task<bool> ReleaseLock(Guid tenantId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReleaseAsync(tenantId, ct);
    }
    
    private async Task<bool> TryAcquireAsync(
        Guid tenantId,
        CancellationToken ct,
        bool storeHandle)
    {
        ct.ThrowIfCancellationRequested();

        var handle = await _lockProvider.TryAcquireLockAsync(GetLockKey(tenantId), _lockTtl, ct);
        if (handle is null)
        {
            return false;
        }

        if (storeHandle)
        {
            _instanceLocks[tenantId] = handle;
        }

        return true;
    }

    private async Task<bool> ReleaseAsync(Guid tenantId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_instanceLocks.TryRemove(tenantId, out var handle))
        {
            return false;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(_releaseTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            _ = linkedCts.Token;

            if (handle is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                handle.Dispose();
            }

            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static string GetLockKey(Guid tenantId) => $"SupersetTenantInstance:{tenantId}";
}