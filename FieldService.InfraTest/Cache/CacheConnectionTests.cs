using FieldService.Cache;
using FieldService.Cache.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.InfraTest.Cache;

public sealed class CacheConnectionTests
{
    [Fact]
    public async Task Should_connect_to_redis_and_resolve_cache_contracts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379,password=redis123",
                ["Redis:InstanceName"] = "FieldService:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCacheModule(configuration);

        using var provider = services.BuildServiceProvider();

        var redisContext = provider.GetRequiredService<IRedisContext>();
        var ping = await redisContext.Database.PingAsync();

        Assert.True(ping >= TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_insert_key_with_two_minutes_ttl_in_redis()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379,password=redis123",
                ["Redis:InstanceName"] = "FieldService:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCacheModule(configuration);

        using var provider = services.BuildServiceProvider();

        var redisContext = provider.GetRequiredService<IRedisContext>();
        var key = $"test:ttl:{Guid.NewGuid():N}";
        var value = $"value:{DateTime.UtcNow:O}";

        var inserted = await redisContext.Database.StringSetAsync(key, value, TimeSpan.FromMinutes(2));
        var readValue = await redisContext.Database.StringGetAsync(key);

        Assert.True(inserted);
        Assert.Equal(value, readValue.ToString());
    }
}
