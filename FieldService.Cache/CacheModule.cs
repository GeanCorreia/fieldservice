using Azure.Identity;
using FieldService.Cache.Configuration;
using FieldService.Cache.Interfaces;
using FieldService.Cache.Services;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace FieldService.Cache;

public static class CacheModule
{
    public static IServiceCollection AddCacheModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        
        services.AddSingleton<IRedisConnection>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var config = sp.GetRequiredService<IConfiguration>();

            var configurationOptions = CreateConfigurationOptions(config, options);
            return new RedisConnection(configurationOptions, options.Database);
        });
        
        services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var redisContext = sp.GetRequiredService<IRedisConnection>();

            return new RedisDistributedSynchronizationProvider(
                redisContext.Connection.GetDatabase());
        });
        
        services.AddFusionCache()
            .WithDefaultEntryOptions(entry =>
            {
                entry.Duration = TimeSpan.FromHours(24);
                entry.IsFailSafeEnabled = false;
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithDistributedCache(sp =>
            {
                var redisContext = sp.GetRequiredService<IRedisConnection>();
                var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

                return new RedisCache(new RedisCacheOptions
                {
                    InstanceName = options.InstanceName,
                    ConnectionMultiplexerFactory = () =>
                        Task.FromResult(redisContext.Connection)
                });
            })
            .WithDistributedLocker(sp =>
            {
                var redisContext = sp.GetRequiredService<IRedisConnection>();

                return new RedisDistributedLocker(
                    new RedisDistributedLockerOptions
                    {
                        ConnectionMultiplexerFactory = () =>
                            Task.FromResult(redisContext.Connection)
                    });
            })
            .AsHybridCache();

        return services;
    }

    private static ConfigurationOptions CreateConfigurationOptions(
        IConfiguration configuration,
        RedisOptions redisOptions)
    {
        var connectionStringName = string.IsNullOrWhiteSpace(redisOptions.ConnectionStringName)
            ? "Redis"
            : redisOptions.ConnectionStringName;

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return ApplyCommonSettings(ConfigurationOptions.Parse(connectionString, ignoreUnknown: true), redisOptions);
        }

        var options = CreateEndpointBasedConfiguration(redisOptions);

        if (redisOptions.UseAzureIdentity)
        {
            options.Ssl = true;
            var credentialOptions = new DefaultAzureCredentialOptions();

            if (!string.IsNullOrWhiteSpace(redisOptions.ManagedIdentityClientId))
            {
                credentialOptions.ManagedIdentityClientId = redisOptions.ManagedIdentityClientId;
            }

            var credential = new DefaultAzureCredential(credentialOptions);
            var azureConfiguredOptions = AzureCacheForRedis
                .ConfigureForAzureWithTokenCredentialAsync(options, credential)
                .GetAwaiter()
                .GetResult();

            return ApplyCommonSettings(azureConfiguredOptions, redisOptions);
        }

        if (!string.IsNullOrWhiteSpace(redisOptions.Password))
        {
            options.Password = redisOptions.Password;
        }

        return ApplyCommonSettings(options, redisOptions);
    }

    private static ConfigurationOptions CreateEndpointBasedConfiguration(RedisOptions redisOptions)
    {
        var options = new ConfigurationOptions();

        if (redisOptions.Endpoints is { Count: > 0 })
        {
            foreach (var endpoint in redisOptions.Endpoints.Where(static endpoint => !string.IsNullOrWhiteSpace(endpoint)))
            {
                options.EndPoints.Add(endpoint);
            }
        }

        if (options.EndPoints.Count == 0 && !string.IsNullOrWhiteSpace(redisOptions.Host))
        {
            options.EndPoints.Add($"{redisOptions.Host}:{redisOptions.Port}");
        }

        if (options.EndPoints.Count == 0)
        {
            throw new InvalidOperationException(
                "Configuração do Redis ausente. Informe ConnectionStrings:Redis ou configure Redis:Host/Redis:Endpoints.");
        }

        options.Ssl = redisOptions.Ssl;

        return options;
    }

    private static ConfigurationOptions ApplyCommonSettings(ConfigurationOptions options, RedisOptions redisOptions)
    {
        options.AbortOnConnectFail = redisOptions.AbortOnConnectFail;

        if (redisOptions.ConnectTimeoutMs > 0)
            options.ConnectTimeout = redisOptions.ConnectTimeoutMs;

        if (redisOptions.SyncTimeoutMs > 0)
            options.SyncTimeout = redisOptions.SyncTimeoutMs;

        return options;
    }
}