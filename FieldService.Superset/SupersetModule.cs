using FieldService.Superset.Configuration;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit; 
using Yarp.ReverseProxy.Configuration;

namespace FieldService.Superset;

public static class SupersetModule
{
    public static IServiceCollection AddSupersetModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupersetOptions>(configuration.GetSection(SupersetOptions.SectionName));
        var supersetOptions = configuration.GetSection(SupersetOptions.SectionName).Get<SupersetOptions>() ?? new SupersetOptions();
        
        
        services.AddRefitClient<ISupersetApi>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        
        services.AddReverseProxy()
            .LoadFromMemory(
                routes: new[]
                {
                    new RouteConfig
                    {
                        RouteId = supersetOptions.RouteId,
                        ClusterId = supersetOptions.ClusterId,
                        Match = new RouteMatch { Path = supersetOptions.RoutePath },
                        Transforms = new List<IReadOnlyDictionary<string, string>>
                        {
                            new Dictionary<string, string> { { "PathRemovePrefix", supersetOptions.PathRemovePrefix } }
                        }
                    }
                },
                clusters: new[]
                {
                    new ClusterConfig
                    {
                        ClusterId = supersetOptions.ClusterId,
                        Destinations = new Dictionary<string, DestinationConfig>
                        {
                            { supersetOptions.DestinationName, new DestinationConfig { Address = supersetOptions.BaseUrl } }
                        }
                    }
                }
            );
        services.AddSingleton<ISupersetTenantInstanceProcessingLock, SupersetTenantInstanceProcessingLock>();
        services.AddScoped<ISupersetAuthService, SupersetAuthService>();
        services.AddScoped<ISupersetTenantDeploymentService, SupersetTenantDeploymentService>();
        services.AddScoped<ISupersetTenantInstanceLifecycleService, SupersetTenantInstanceLifecycleService>();
        services.AddScoped<ISupersetResourceService, SupersetResourceService>();
        return services;
    }
    
    public static IApplicationBuilder UseSupersetProxy(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapReverseProxy();
        });

        return app;
    }
}