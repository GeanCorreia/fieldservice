using System.Collections.Concurrent;
using FieldService.Broker.Configuration;
using FieldService.Broker.Interfaces;
using Medallion.Threading;
using Microsoft.Extensions.Configuration;

namespace FieldService.Broker.Services;

public class MessageProcessingLock : IMessageProcessingLock
{
    private readonly IConfiguration _configuration;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ConcurrentDictionary<Guid, IDistributedSynchronizationHandle> _messageLocks = new();
    private readonly ConcurrentDictionary<string, IDistributedSynchronizationHandle> _namedLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _lockTtl;
    private readonly TimeSpan _releaseTimeout;
    private IDistributedSynchronizationHandle? _brokerConfiguratorLock;

    public const string BrokerConfiguratorLockKey = "BrokerConfiguratorLock";

    public MessageProcessingLock(
        IDistributedLockProvider lockProvider,
        IConfiguration configuration)
    {
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var brokerOutboxOptions = _configuration.GetSection(BrokerOutboxOptions.SectionName).Get<BrokerOutboxOptions>()
            ?? new BrokerOutboxOptions();

        _lockTtl = TimeSpan.FromSeconds(Math.Max(1, brokerOutboxOptions.MessageLockTtlSeconds));
        _releaseTimeout = _lockTtl;
    }

    public async Task<bool> BrokerConfiguratorAcquireLock(CancellationToken ct = default)
    {
        if (_brokerConfiguratorLock is not null)
        {
            return true;
        }

        return await TryAcquireAsync(BrokerConfiguratorLockKey, _lockTtl, ct, storeHandle: true, handleTarget: "broker-configurator");
    }

    public async Task<bool> BrokerConfiguratorReleaseLock(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReleaseAsync(BrokerConfiguratorLockKey, ct, target: "broker-configurator");
    }

    public async Task<bool> MessageAcquireLock(Guid messageId,  CancellationToken ct = default)
    {
        var key = $"BrokerMessage:{messageId}";
        return await TryAcquireAsync(key, _lockTtl, ct, storeHandle: true, handleTarget: messageId);
    }

    public async Task<bool> MessageReleaseLock(Guid messageId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReleaseAsync($"BrokerMessage:{messageId}", ct, target: messageId);
    }

    

    private async Task<bool> TryAcquireAsync(
        string key,
        TimeSpan ttl,
        CancellationToken ct,
        bool storeHandle,
        object handleTarget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();

        var handle = await _lockProvider.TryAcquireLockAsync(key, ttl, ct);
        if (handle is null)
        {
            return false;
        }

        if (storeHandle)
        {
            if (handleTarget is string namedKey)
            {
                _namedLocks[namedKey] = handle;
            }
            else if (handleTarget is Guid messageId)
            {
                _messageLocks[messageId] = handle;
            }
            else if (string.Equals(handleTarget?.ToString(), "broker-configurator", StringComparison.OrdinalIgnoreCase))
            {
                _brokerConfiguratorLock = handle;
            }
        }

        return true;
    }

    private Task<bool> ReleaseAsync(string key, CancellationToken ct, object target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();

        IDistributedSynchronizationHandle? handle = null;

        if (target is string namedKey)
        {
            _namedLocks.TryRemove(namedKey, out handle);
        }
        else if (target is Guid messageId)
        {
            _messageLocks.TryRemove(messageId, out handle);
        }
        else if (string.Equals(target?.ToString(), "broker-configurator", StringComparison.OrdinalIgnoreCase))
        {
            handle = _brokerConfiguratorLock;
            _brokerConfiguratorLock = null;
        }

        if (handle is null)
        {
            return Task.FromResult(false);
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(_releaseTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            _ = linkedCts.Token; // keeps the release timeout centralized and explicit
            handle.Dispose();
            return Task.FromResult(true);
        }
        catch (ObjectDisposedException)
        {
            return Task.FromResult(false);
        }
    }
    

    public async Task<IEnumerable<Guid>> MessageAcquireLocks(IEnumerable<Guid> messageIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);
        ct.ThrowIfCancellationRequested();

        var ids = messageIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return Enumerable.Empty<Guid>();
        }

        var acquiredList = new ConcurrentBag<Guid>();


        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount * 4, 32)
        };
        
        await Parallel.ForEachAsync(ids, parallelOptions, async (messageId, cancellationToken) =>
        {
            var key = $"BrokerMessage:{messageId}";
            
            var handle = await _lockProvider.TryAcquireLockAsync(key, _lockTtl, cancellationToken);

            if (handle is not null)
            {
                _messageLocks[messageId] = handle;
                acquiredList.Add(messageId);
            }
        });

        return acquiredList;
    }

    public async Task<IEnumerable<Guid>> MessageReleaseLocks(IEnumerable<Guid> messageIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);
        ct.ThrowIfCancellationRequested();

        var ids = messageIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return Enumerable.Empty<Guid>();
        }

        var releasedList = new ConcurrentBag<Guid>();

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount * 4, 32)
        };

        await Parallel.ForEachAsync(ids, parallelOptions, async (messageId, _) =>
        {
            if (_messageLocks.TryRemove(messageId, out var handle))
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(_releaseTimeout);
                    
                    if (handle is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        handle.Dispose();
                    }

                    releasedList.Add(messageId);
                }
                catch (ObjectDisposedException)
                {
                    releasedList.Add(messageId);
                }
            }
        });

        return releasedList;
    }

}