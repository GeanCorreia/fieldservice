using FieldService.Shared.Configuration;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Shared;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.Configure<AzureIdentityOptions>(configuration.GetSection(AzureIdentityOptions.SectionName));

        services.AddSingleton<IDateTimeService, DateTimeService>();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("FieldService") == true)
            .ToArray();
        
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        services.AddScoped<EncryptionService>();
        services.AddValidatorsFromAssemblies(assemblies);
        
        return services;
    }
}