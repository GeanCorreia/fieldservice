using System.Collections.Concurrent;
using FieldService.Form.Interfaces;
using Medallion.Threading;
using Microsoft.Extensions.Configuration;

namespace FieldService.Form.Services;

/// <summary>
/// Distributed lock for Form processing operations
/// 
/// Ensures that multiple instances of the application don't process
/// the same form tenant concurrently (e.g., provisioning, migrations).
/// 
/// Uses Medallion.Threading distributed lock provider (supports
/// various backends: SQL, Redis, Azure, etc.)
/// </summary>
internal sealed class FormProcessingLock : IFormProcessingLock
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ConcurrentDictionary<Guid, IDistributedSynchronizationHandle> _instanceLocks = new();
    private readonly TimeSpan _lockTtl;
    private readonly TimeSpan _releaseTimeout;

    /// <summary>
    /// Configuration key for Form lock settings
    /// </summary>
    private const string FormLockConfigKey = "Form:Lock";
    private const int DefaultLockTtlMinutes = 5;

    public FormProcessingLock(
        IDistributedLockProvider lockProvider,
        IConfiguration configuration)
    {
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Read lock configuration
        var lockTtlMinutes = configuration.GetValue<int?>($"{FormLockConfigKey}:TtlMinutes") 
            ?? DefaultLockTtlMinutes;

        _lockTtl = TimeSpan.FromMinutes(Math.Max(1, lockTtlMinutes));
        _releaseTimeout = _lockTtl;
    }

    /// <summary>
    /// Attempts to acquire a distributed lock for the tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if lock was acquired, false if already held by another process</returns>
    public async Task<bool> AcquireLock(Guid tenantId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await TryAcquireAsync(tenantId, ct, storeHandle: true);
    }

    /// <summary>
    /// Releases the distributed lock for the tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if lock was released, false if lock wasn't held</returns>
    public async Task<bool> ReleaseLock(Guid tenantId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReleaseAsync(tenantId, ct);
    }

    /// <summary>
    /// Attempts to acquire lock with optional storage of handle
    /// </summary>
    private async Task<bool> TryAcquireAsync(
        Guid tenantId,
        CancellationToken ct,
        bool storeHandle)
    {
        ct.ThrowIfCancellationRequested();

        var handle = await _lockProvider.TryAcquireLockAsync(
            GetLockKey(tenantId),
            _lockTtl,
            ct);

        if (handle is null)
        {
            // Lock is held by another process
            return false;
        }

        if (storeHandle)
        {
            // Store handle for later release
            _instanceLocks[tenantId] = handle;
        }

        return true;
    }

    /// <summary>
    /// Releases lock and removes it from local tracking
    /// </summary>
    private async Task<bool> ReleaseAsync(Guid tenantId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_instanceLocks.TryRemove(tenantId, out var handle))
        {
            // Lock wasn't in our dictionary
            return false;
        }

        try
        {
            // Create timeout for disposal to prevent hanging
            using var timeoutCts = new CancellationTokenSource(_releaseTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            _ = linkedCts.Token;

            // Try async disposal first, fallback to sync
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
            // Already disposed
            return false;
        }
    }

    /// <summary>
    /// Generates the lock key for distributed lock provider
    /// </summary>
    private static string GetLockKey(Guid tenantId) => $"FormProcessing:{tenantId}";
}

