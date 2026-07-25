using FieldService.Broker.Interfaces;
using FieldService.Cache.Interfaces;

namespace FieldService.Cache.Services;

internal sealed class RedisMessageProcessingLock(IRedisContext redisContext) : IMessageProcessingLock
{
    private readonly StackExchange.Redis.IDatabase _database = redisContext.Database;

    public async Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();

        return await _database.StringSetAsync(key, "1", ttl, StackExchange.Redis.When.NotExists);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();

        await _database.KeyDeleteAsync(key);
    }
}
