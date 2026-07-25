using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Data;

public static class DataModule
{
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
