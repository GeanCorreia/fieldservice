using System.Reflection;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Services;
using FluentValidation;
using MediatR.Extensions.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Shared;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<IHashService, HashService>();
        services.AddScoped<IRequestContextManager, RequestContextManager>();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("FieldService") == true)
            .ToArray();
        
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });
        
        services.AddValidatorsFromAssemblies(assemblies);
        services.AddFluentValidation(new[] { typeof(SharedModule).Assembly });
        
        return services;
    }
}
