using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Storage.Factories;

public class StorageProviderFactory(
    IServiceProvider serviceProvider,
    IWebHostEnvironment environment) : IStorageProviderFactory
{
    public IStorageProviderService GetProvider(StorageProvider provider)
    {
        return provider switch
        {
            StorageProvider.AmazonS3 =>
                serviceProvider.GetRequiredService<AmazonS3Service>(),

            StorageProvider.AzureBlob =>
                environment.IsDevelopment()
                    ? serviceProvider.GetRequiredService<DevelopmentAzureStorageService>()
                    : serviceProvider.GetRequiredService<AzureStorageService>(),

            StorageProvider.GoogleCloudStorage =>
                serviceProvider.GetRequiredService<GoogleCloudStorageService>(),

            _ => throw new NotSupportedException(
                $"Storage provider '{provider}' is not supported.")
        };
    }
}