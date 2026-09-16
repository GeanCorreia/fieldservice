using FieldService.Superset.Configuration;
using FieldService.Superset.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit; 

namespace FieldService.Superset;

public static class SupersetModule
{
    public static IServiceCollection AddSupersetModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupersetOptions>(configuration.GetSection(SupersetOptions.SectionName));
        
        services.AddRefitClient<ISupersetApi>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SupersetOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
        

        return services;
    }
}