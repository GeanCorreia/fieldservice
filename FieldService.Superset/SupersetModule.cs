using FieldService.Superset.Attributes;
using FieldService.Superset.Configuration;
using FieldService.Superset.Data.Repositories;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Proxy;
using FieldService.Superset.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

        services.AddRefitClient<ISupersetUserManagement>()
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
                        AuthorizationPolicy = SupersetPermissions.Access,
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
        services.AddScoped<ISupersetService, SupersetService>();
        services.AddScoped<ISupersetAuthService, SupersetAuthService>();
        services.AddScoped<ISupersetRepository, SupersetRepository>();
        services.AddScoped<ISupersetSecretService, SupersetSecretService>();
        services.AddScoped<ISupersetTenantDeploymentService, SupersetTenantDeploymentService>();
        services.AddScoped<ISupersetTenantContainerConfigurationService, SupersetTenantContainerConfigurationService>();
        services.AddScoped<ISupersetTenantInstanceLifecycleService, SupersetTenantInstanceLifecycleService>();
        services.AddScoped<SupersetContainerAllowedOriginsCors>();
        return services;
    }
    
    public static IEndpointRouteBuilder UseSupersetProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapReverseProxy();
        return endpoints;
    }
    
    public static IServiceCollection AddSupersetProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var supersetOptions = configuration.GetSection("Superset").Get<SupersetOptions>();

        services.AddReverseProxy()
            .LoadFromMemory(
                routes: new[]
                {
                    new RouteConfig
                    {
                        RouteId = supersetOptions.RouteId,
                        ClusterId = "dynamic-superset-cluster",
                        Match = new RouteMatch { Path = "/api/v1/superset/{**catch-all}" },
                        Transforms = new List<IReadOnlyDictionary<string, string>>
                        {
                            new Dictionary<string, string> { { "PathRemovePrefix", "/api/v1/superset" } }
                        }
                    }
                },
                clusters: new[]
                {
                    new ClusterConfig
                    {
                        ClusterId = "dynamic-superset-cluster",
                        Destinations = new Dictionary<string, DestinationConfig>
                        {
                            { "default", new DestinationConfig { Address = "http://localhost" } }
                        }
                    }
                }
            )
            .AddTransforms<SupersetTenantDynamicTransformProvider>();

        return services;
    }
}