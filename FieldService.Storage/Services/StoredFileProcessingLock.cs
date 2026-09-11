using System.Collections.Concurrent;
using FieldService.Storage.Configuration;
using FieldService.Storage.Interfaces;
using Medallion.Threading;
using Microsoft.Extensions.Configuration;

namespace FieldService.Storage.Services;

internal class StoredFileProcessingLock : IStoredFileProcessingLock
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ConcurrentDictionary<Guid, IDistributedSynchronizationHandle> _storedFileLocks = new();
    private readonly TimeSpan _lockTtl;
    private readonly TimeSpan _releaseTimeout;
    private readonly int _maxParallelism;

    public StoredFileProcessingLock(
        IDistributedLockProvider lockProvider,
        IConfiguration configuration)
    {
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();

        _lockTtl = TimeSpan.FromSeconds(Math.Max(1, storageOptions.StoredFileLockTtlSeconds));
        _releaseTimeout = _lockTtl;
        _maxParallelism = Math.Max(1, storageOptions.MaxParallelism);
    }

    public async Task<bool> StoredFileAcquireLock(Guid storedFileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await TryAcquireAsync(storedFileId, ct, storeHandle: true);
    }

    public async Task<bool> StoredFileReleaseLock(Guid storedFileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReleaseAsync(storedFileId, ct);
    }

    public async Task<IEnumerable<Guid>> StoredFileAcquireLocks(IEnumerable<Guid> storedFileIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFileIds);
        ct.ThrowIfCancellationRequested();

        var ids = storedFileIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Enumerable.Empty<Guid>();
        }

        var acquiredList = new ConcurrentBag<Guid>();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = _maxParallelism
        };

        await Parallel.ForEachAsync(ids, parallelOptions, async (storedFileId, cancellationToken) =>
        {
            if (await TryAcquireAsync(storedFileId, cancellationToken, storeHandle: true))
            {
                acquiredList.Add(storedFileId);
            }
        });

        return acquiredList;
    }

    public async Task<IEnumerable<Guid>> StoredFileReleaseLocks(IEnumerable<Guid> storedFileIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFileIds);
        ct.ThrowIfCancellationRequested();

        var ids = storedFileIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Enumerable.Empty<Guid>();
        }

        var releasedList = new ConcurrentBag<Guid>();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = _maxParallelism
        };

        await Parallel.ForEachAsync(ids, parallelOptions, async (storedFileId, _) =>
        {
            if (await ReleaseAsync(storedFileId, ct))
            {
                releasedList.Add(storedFileId);
            }
        });

        return releasedList;
    }

    private async Task<bool> TryAcquireAsync(
        Guid storedFileId,
        CancellationToken ct,
        bool storeHandle)
    {
        ct.ThrowIfCancellationRequested();

        var handle = await _lockProvider.TryAcquireLockAsync(GetLockKey(storedFileId), _lockTtl, ct);
        if (handle is null)
        {
            return false;
        }

        if (storeHandle)
        {
            _storedFileLocks[storedFileId] = handle;
        }

        return true;
    }

    private async Task<bool> ReleaseAsync(Guid storedFileId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_storedFileLocks.TryRemove(storedFileId, out var handle))
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

    private static string GetLockKey(Guid storedFileId) => $"StoredFile:{storedFileId}";
}
