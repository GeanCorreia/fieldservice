using FieldService.Audit.Channels;
using FieldService.Audit.Services;
using FieldService.Data;
using FiledService.Audit.Data.Repositories;
using FiledService.Audit.Data;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiledService.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlModule<AuditDbContext>(configuration);
        services.AddScoped<IAuditChangeRepository, AuditRepository>();
        services.AddScoped<IAuditDtoSchemaRepository, AuditDtoSchemaRepository>();
        services.AddScoped<IAuditAccessService, AuditAccessService>();
        services.AddScoped<IAuditChangeService, AuditChangeService>();
        services.AddScoped<IAuditFallbackService, AuditFallbackService>();
        services.AddScoped<IAuditChangeFallbackService>(serviceProvider =>
            (IAuditChangeFallbackService)serviceProvider.GetRequiredService<IAuditFallbackService>());
        services.AddScoped<IAuditDtoSchemaBootstrapService, AuditDtoSchemaBootstrapService>();
        services.AddScoped<IAuditRequestRepository, AuditRequestRepository>();
        services.AddScoped<IAuditRequestFallbackService>(serviceProvider =>
            (IAuditRequestFallbackService)serviceProvider.GetRequiredService<IAuditFallbackService>());

        services.AddSingleton<AuditAccessChannel>();
        services.AddSingleton<AuditChangeChannel>();
        services.AddSingleton<AuditRequestChannel>();
        services.AddHostedService<AuditDtoSchemaBootstrapHostedService>();
        services.AddHostedService<AuditAccessRegistryService>();
        services.AddHostedService<AuditChangeRegistryService>();
        services.AddHostedService<AuditRequestRegistryService>();
        return services;
    }
}
