using FieldService.Cache.Interfaces;
using StackExchange.Redis;

namespace FieldService.Cache.Services;

internal sealed class RedisConnection : IRedisConnection, IDisposable, IAsyncDisposable
{
    private readonly ConnectionMultiplexer _multiplexer;

    public RedisConnection(ConfigurationOptions configurationOptions, int database)
    {
        _multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        Database = database >= 0
            ? _multiplexer.GetDatabase(database)
            : _multiplexer.GetDatabase();
    }

    public IDatabase Database { get; }
    public IConnectionMultiplexer Connection => _multiplexer;

    public void Dispose()
    {
        _multiplexer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _multiplexer.DisposeAsync();
    }
}