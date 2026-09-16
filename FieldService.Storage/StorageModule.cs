using FieldService.Storage.Channels;
using FieldService.Storage.Configuration;
using FieldService.Storage.Data;
using FieldService.Storage.Data.Repositories;
using FieldService.Storage.Factories;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Extensions;
using FieldService.Storage.Services;
using FieldService.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddSqlModule<StorageDbContext>(configuration);

        services.AddScoped<StoredFileRepository>();
        services.AddScoped<IStoredFileRepository>(sp => sp.GetRequiredService<StoredFileRepository>());
        services.AddScoped<ICleanUpStoredFileRepository>(sp => sp.GetRequiredService<StoredFileRepository>());
        
        services.AddScoped<RedisStoredFileService>();
        services.AddScoped<IStoredFileCacheService>(sp => sp.GetRequiredService<RedisStoredFileService>());

        services.AddScoped<StoredFileService>();
        services.AddScoped<IStoredFileService>(sp => sp.GetRequiredService<StoredFileService>());

        services.AddScoped<StorageService>();
        services.AddScoped<IStorageService>(sp => sp.GetRequiredService<StorageService>());

        services.AddScoped<StoragePresignedUrlService>();
        services.AddScoped<IStoragePresignedUrlService>(sp => sp.GetRequiredService<StoragePresignedUrlService>());

        services.AddSingleton<AmazonS3Service>();
        services.AddSingleton<DevelopmentAzureStorageService>();
        services.AddSingleton<AzureStorageService>();
        services.AddSingleton<GoogleCloudStorageService>();
        services.AddSingleton<StorageProviderFactory>();
        services.AddSingleton<IStorageProviderFactory, StorageProviderFactory>();
        services.AddSingleton<StoredFileProcessingLock>();

        services.AddScoped<StoredFileCategoryBootstrapService>();
        services.AddSingleton<StoredFileCanceledUploadOutboxChannel>();
        services.AddSingleton<StoredFileFailedUploadOutboxChannel>();
        services.AddHostedService<StoredFileFailedUploadOutboxService>();
        services.AddHostedService<StoredFileCanceledUploadOutboxService>();

        return services;
    }
}