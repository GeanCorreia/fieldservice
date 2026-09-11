using FieldService.Cache.Interfaces;
using FieldService.Cache.Services;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FieldService.Cache;

public static class CacheModule
{
    public static IServiceCollection AddCacheModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        var configurationOptions = CreateConfigurationOptions(configuration, redisOptions);

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = configurationOptions;
            options.InstanceName = redisOptions.InstanceName;
        });
        services.AddSingleton<IRedisContext>(_ => new RedisContext(configurationOptions, redisOptions.Database));
        services.AddSingleton<IDistributedLockProvider>(_ =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            return new RedisDistributedSynchronizationProvider(multiplexer.GetDatabase(redisOptions.Database));
        });

        return services;
    }

    private static ConfigurationOptions CreateConfigurationOptions(IConfiguration configuration, RedisOptions redisOptions)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        ConfigurationOptions configurationOptions;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            configurationOptions = ConfigurationOptions.Parse(connectionString, ignoreUnknown: true);
        }
        else if (redisOptions.Endpoints is { Count: > 0 })
        {
            configurationOptions = new ConfigurationOptions();
            foreach (var endpoint in redisOptions.Endpoints)
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    continue;

                configurationOptions.EndPoints.Add(endpoint.Trim());
            }
        }
        else
        {
            throw new InvalidOperationException("Configure Redis using ConnectionStrings:Redis or Redis:Endpoints.");
        }

        configurationOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
        configurationOptions.Ssl = redisOptions.Ssl;

        if (redisOptions.ConnectTimeoutMs > 0)
            configurationOptions.ConnectTimeout = redisOptions.ConnectTimeoutMs;

        if (redisOptions.SyncTimeoutMs > 0)
            configurationOptions.SyncTimeout = redisOptions.SyncTimeoutMs;

        if (!string.IsNullOrWhiteSpace(redisOptions.Password))
            configurationOptions.Password = redisOptions.Password;

        return configurationOptions;
    }

    
}



public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string InstanceName { get; init; } = "FieldService:";
    public int Database { get; init; } = -1;
    public List<string>? Endpoints { get; init; }
    public string? Password { get; init; }
    public bool Ssl { get; init; }
    public bool AbortOnConnectFail { get; init; } = false;
    public int ConnectTimeoutMs { get; init; } = 5000;
    public int SyncTimeoutMs { get; init; } = 5000;
}
