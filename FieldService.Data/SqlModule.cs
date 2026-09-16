using FieldService.Data.Interfaces;
using FieldService.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FieldService.Data;

public static class SqlModule
{
    public static IServiceCollection AddSqlModule<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment,
        string connectionStringName = "Postgres")
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var environmentName = environment?.EnvironmentName ?? "Unknown";
        if (string.IsNullOrWhiteSpace(connectionStringName))
            throw new ArgumentException("Connection string name is required.", nameof(connectionStringName));

        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionStringName} is not configured for environment '{environmentName}'.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"ConnectionStrings:{connectionStringName} is empty for environment '{environmentName}'.");

        services.TryAddScoped<IEntityChangeExtractor, EntityChangeExtractor>();
        services.TryAddScoped<IEntityChangeCollector, EntityChangeCollector>();
        services.AddDbContext<TDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ISqlUnitOfWork<TDbContext>, SqlUnitOfWork<TDbContext>>();
        services.TryAddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ISqlUnitOfWork<TDbContext>>());

        return services;
    }

    public static IServiceCollection AddSqlModule<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Postgres")
        where TDbContext : DbContext
    {
        return services.AddSqlModule<TDbContext>(configuration, environment: null, connectionStringName);
    }

    public static IServiceCollection AddSqlModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSqlModule<SqlDbContext>(configuration, environment: null);
    }

    public static IServiceCollection AddSqlModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        return services.AddSqlModule<SqlDbContext>(configuration, environment);
    }
}
