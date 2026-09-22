using StackExchange.Redis;

namespace FieldService.Cache.Interfaces;

public interface IRedisConnection
{
    IConnectionMultiplexer Connection { get; }
}
