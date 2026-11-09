using System.Collections.Concurrent;
using FieldService.Cloud.Interfaces;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace FieldService.Cloud.Services;

public sealed class ContainerProcessingLock(
    IDistributedLockProvider lockProvider,
    ILogger<ContainerProcessingLock> logger) : IContainerProcessingLock
{
    private readonly IDistributedLockProvider _lockProvider =
        lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
    private readonly ILogger<ContainerProcessingLock> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<string, IDistributedSynchronizationHandle> _handles = new();

    public async Task<bool> AcquireAsync(
        string key,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        var handle = await _lockProvider.TryAcquireLockAsync(key, timeout, cancellationToken);
        if (handle is null)
            return false;

        if (_handles.TryAdd(key, handle))
            return true;

        await handle.DisposeAsync();
        return false;
    }

    public async Task<bool> ReleaseAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_handles.TryRemove(key, out var handle))
            return false;

        try
        {
            await handle.DisposeAsync();
            return true;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Container processing lock {LockKey} was already disposed.", key);
            return false;
        }
    }
}
