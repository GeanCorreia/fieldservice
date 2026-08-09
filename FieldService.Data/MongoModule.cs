using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using FieldService.Data.Contexts;
using FieldService.Data.Interfaces;
using FieldService.Data.Services;

namespace FieldService.Data;

public static class MongoModule
{
    public static IServiceCollection AddMongoModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddMongoModule(configuration, environment: null);
    }

    public static IServiceCollection AddMongoModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var environmentName = environment?.EnvironmentName ?? "Unknown";
        var connectionString = configuration.GetConnectionString("MongoDb")
                               ?? throw new InvalidOperationException($"ConnectionStrings:MongoDb is not configured for environment '{environmentName}'.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"ConnectionStrings:MongoDb is empty for environment '{environmentName}'.");

        var mongoOptions = configuration.GetSection(MongoDbOptions.SectionName).Get<MongoDbOptions>()
                           ?? throw new InvalidOperationException($"MongoDb section is not configured for environment '{environmentName}'.");
        if (string.IsNullOrWhiteSpace(mongoOptions.Database))
            throw new InvalidOperationException($"MongoDb:Database is not configured for environment '{environmentName}'.");

        services.TryAddScoped<IEntityChangeExtractor, EntityChangeExtractor>();
        services.TryAddScoped<IEntityChangeCollector, EntityChangeCollector>();
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabaseNameResolver, MongoDatabaseNameResolver>();
        services.AddScoped<IMongoUnitOfWork, MongoUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IMongoUnitOfWork>());
        services.AddScoped<IMongoReadDbContextFactory, MongoReadDbContextFactory>();
        services.AddScoped<IMongoWriteDbContextFactory, MongoWriteDbContextFactory>();
        services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return client.GetDatabase(options.Database);
        });

        return services;
    }
}

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string Database { get; init; } = string.Empty;
    public Dictionary<string, string>? ModuleDatabases { get; init; }
}
