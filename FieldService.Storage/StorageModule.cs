using FieldService.Storage.Configuration;
using FieldService.Storage.Data;
using FieldService.Storage.Data.Repositories;
using FieldService.Storage.Factories;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Extensions;
using FieldService.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace FieldService.Storage;

public static class StorageModule
{
    public static IServiceCollection AddStorageModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<StorageDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<StoredFileRepository>();
        services.AddScoped<IStoredFileRepository>(sp => sp.GetRequiredService<StoredFileRepository>());
        services.AddScoped<RedisStoredFileService>();
        services.AddScoped<IStoredFileCacheService>(sp => sp.GetRequiredService<RedisStoredFileService>());

        services.AddScoped<StoredFileService>();
        services.AddScoped<IStoredFileService>(sp => sp.GetRequiredService<StoredFileService>());

        services.AddScoped<StorageService>();
        services.AddScoped<IStorageService>(sp => sp.GetRequiredService<StorageService>());

        services.AddScoped<StoragePresignedUrlService>();
        services.AddScoped<IStoragePresignedUrlService>(sp => sp.GetRequiredService<StoragePresignedUrlService>());

        services.AddSingleton<AmazonS3Service>();
        services.AddSingleton<AzureStorageService>();
        services.AddSingleton<GoogleCloudStorageService>();
        services.AddSingleton<StoredFileProcessingLock>();

        services.AddSingleton<StorageProviderFactory>();
        services.AddScoped<StoredFileCategoryBootstrapService>();
        services.AddHostedService<StoredFileCategoryBootstrapHostedService>();

        return services;
    }
}