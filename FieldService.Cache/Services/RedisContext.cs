using FieldService.Cache.Interfaces;
using StackExchange.Redis;

namespace FieldService.Cache.Services;

internal sealed class RedisContext : IRedisContext, IDisposable
{
    private readonly ConnectionMultiplexer _multiplexer;

    public RedisContext(ConfigurationOptions configurationOptions, int database)
    {
        _multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        Database = database >= 0
            ? _multiplexer.GetDatabase(database)
            : _multiplexer.GetDatabase();
    }

    public IDatabase Database { get; }

    public void Dispose()
    {
        _multiplexer.Dispose();
    }
}
