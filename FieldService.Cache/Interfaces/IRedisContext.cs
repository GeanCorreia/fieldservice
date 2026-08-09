using StackExchange.Redis;

namespace FieldService.Cache.Interfaces;

public interface IRedisContext
{
    IDatabase Database { get; }
    IConnectionMultiplexer Connection { get; }
}
