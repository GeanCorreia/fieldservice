using FieldService.Data.Interfaces;
using FieldService.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Data;

public static class SqlModule
{
    public static IServiceCollection AddSqlModule<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Postgres")
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(connectionStringName))
            throw new ArgumentException("Connection string name is required.", nameof(connectionStringName));

        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionStringName} is not configured.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"ConnectionStrings:{connectionStringName} is empty.");

        services.AddDbContext<TDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ISqlUnitOfWork<TDbContext>, SqlUnitOfWork<TDbContext>>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ISqlUnitOfWork<TDbContext>>());

        return services;
    }

    public static IServiceCollection AddSqlModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSqlModule<SqlDbContext>(configuration);
    }
}
