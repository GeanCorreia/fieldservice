using Microsoft.Extensions.DependencyInjection;

namespace FieldService.DocumentSupportManagement;

public static class DsmModule
{
    public static IServiceCollection AddDsmModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddControllers()
            .AddApplicationPart(typeof(DsmModule).Assembly);

        return services;
    }
}