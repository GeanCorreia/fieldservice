using Azure.Security.KeyVault.Secrets;
using FieldService.Data;
using FieldService.SecretKey.Configuration;
using FieldService.SecretKey.Data;
using FieldService.SecretKey.Data.Repositories;
using FieldService.SecretKey.Interfaces;
using FieldService.SecretKey.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace FieldService.SecretKey;

public static class SecretKeyModule
{
    public static IServiceCollection AddSecretKeyModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {

        services.Configure<SecretKeyOptions>(
            configuration.GetSection(SecretKeyOptions.SectionName));

        
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SecretKeyOptions>>().Value;
            
            if (string.IsNullOrEmpty(options.KeyVaultEndpoint))
                throw new InvalidOperationException("Azure Key Vault Endpoint não configurado em SecretKeyOptions.");
            
            return new SecretClient(new Uri(options.KeyVaultEndpoint), new global::Azure.Identity.DefaultAzureCredential());
        });
        
        services.AddScoped<ISecretKeyService, SecretKeyService>();
        services.AddScoped<ISecretKeyVaultService, SecretKeyVaultService>();
        services.AddScoped<ISecretKeyRepository, SecretKeyRepository>();
        services.AddSqlModule<SecretKeyDbContext>(configuration);

        return services;
    }
}