using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.ResourceManager;
using FieldService.Data;
using FieldService.Cloud.Configuration;
using FieldService.Cloud.Data;
using FieldService.Cloud.Data.Repositories;
using FieldService.Cloud.Interfaces;
using FieldService.Cloud.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
namespace FieldService.Cloud;

public static class CloudModule
{
    public static IServiceCollection AddCloudModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.Configure<SecretKeyOptions>(
            configuration.GetSection(SecretKeyOptions.SectionName));

        services.TryAddSingleton<ArmClient>(_ => new ArmClient(new DefaultAzureCredential()));
        services.TryAddSingleton<IAzureContainerService, AzureContainerService>();
        services.TryAddSingleton<IAzureContainerConfigurationService, AzureContainerConfigurationService>();
        services.TryAddSingleton<IContainerProcessingLock, ContainerProcessingLock>();


        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SecretKeyOptions>>().Value;

            if (string.IsNullOrEmpty(options.KeyVaultEndpoint))
                throw new InvalidOperationException("Azure Key Vault Endpoint não configurado em SecretKeyOptions.");

            var clientOptions = new SecretClientOptions();

            if (options.KeyVaultEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                clientOptions.DisableChallengeResourceVerification = true;
            }

            return new SecretClient(
                new Uri(options.KeyVaultEndpoint),
                new DefaultAzureCredential(),
                clientOptions);
        });

        services.AddScoped<ISecretKeyService, SecretKeyService>();
        services.AddScoped<ISecretKeyVaultService, SecretKeyVaultService>();
        services.AddScoped<ISecretKeyRepository, SecretKeyRepository>();
        services.AddScoped<IContainerRepository, ContainerRepository>();
        services.AddSqlModule<CloudDbContext>(configuration);

        return services;
    }
}