using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Data;

public static class DataModule
{
    public static IServiceCollection AddDataModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddMongoModule(configuration, environment);
        services.AddSqlModule(configuration, environment);

        return services;
    }

    public static IServiceCollection AddDataModule(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder, string> configureProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureProvider);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<FieldServiceDbContext>(options => configureProvider(options, connectionString));

        return services;
    }
}
