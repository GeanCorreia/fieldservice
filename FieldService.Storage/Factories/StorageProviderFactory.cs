using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Storage.Factories;

public class StorageProviderFactory(IServiceProvider serviceProvider) : IStorageProviderFactory
{
    public IStorageProviderService GetProvider(StorageProvider provider)
    {
        return provider switch
        {
            StorageProvider.AmazonS3 => serviceProvider.GetRequiredService<AmazonS3Service>(),
            StorageProvider.AzureBlob => serviceProvider.GetRequiredService<AzureStorageService>(),
            StorageProvider.GoogleCloudStorage => serviceProvider.GetRequiredService<GoogleCloudStorageService>(),
            _ => throw new NotSupportedException($"Storage provider '{provider}' is not supported.")
        };
    }
}