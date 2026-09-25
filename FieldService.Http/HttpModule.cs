using System.Text.Json.Serialization.Metadata;
using FieldService.Http.PipeLine;
using FieldService.Shared.Dtos;
using FieldService.Http.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Http;

public static class HttpModule
{
    private const int AuditChangeFilterOrder = -100;
    private const int AuditAccessFilterOrder = 0;
    private const int AuditRequestFilterOrder = 1;
    private const int ApiResponseFilterOrder = 100;

    public static WebApplicationBuilder UseHttpPipeline(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddHttpPipeline(builder.Services);
        return builder;
    }

    private static void AddHttpPipeline(IServiceCollection services)
    {
        services.AddScoped<HttpApiResponseFilter>();
        services.AddScoped<HttpAuditAccessFilter>();
        services.AddScoped<HttpAuditChangeFilter>();
        services.AddScoped<HttpAuditRequestFilter>();
        services.Configure<MvcOptions>(ConfigureFilterOrder);
        services.Configure<JsonOptions>(ConfigureJsonSerialization);
        SwaggerModuleDiscovery.AddSwagger(services);
        
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var existingOnMessageReceived = options.Events?.OnMessageReceived;

            options.Events ??= new JwtBearerEvents();
            options.Events.OnMessageReceived = async context =>
            {
                if (existingOnMessageReceived != null)
                {
                    await existingOnMessageReceived(context);
                }

                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/signalr"))
                {
                    context.Token = accessToken;
                }
            };
        });
        
        services.AddCors(options =>
        {
            options.AddPolicy("SignalRCorsPolicy", policy =>
            {
                policy.SetIsOriginAllowed(_ => true) 
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); 
            });
        });
    }

    public static WebApplication UseHttpPipeline(
        this WebApplication app,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var module in SwaggerModuleDiscovery.DiscoverModulesWithVersions())
                    options.SwaggerEndpoint(
                        $"/swagger/{module.ModuleName}/swagger.json",
                        $"FieldService {module.ModuleName} ({module.SwaggerVersion})");
            });
        }

        app.UseRouting();
        app.UseCors("SignalRCorsPolicy");
        app.UseMiddleware<RequestIdMiddleware>();
        
        if (!environment.IsDevelopment())
            app.UseAuthentication();
        
        app.UseMiddleware<HttpAuthenticationMiddleware>();
        app.UseMiddleware<HttpObservabilityMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        return app;
    }

    private static void ConfigureFilterOrder(MvcOptions options)
    {
        options.Filters.AddService<HttpAuditChangeFilter>(AuditChangeFilterOrder);
        options.Filters.AddService<HttpAuditAccessFilter>(AuditAccessFilterOrder);
        options.Filters.AddService<HttpAuditRequestFilter>(AuditRequestFilterOrder);
        options.Filters.AddService<HttpApiResponseFilter>(ApiResponseFilterOrder);
    }

    private static void ConfigureJsonSerialization(JsonOptions options)
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (!typeof(AbstractDto).IsAssignableFrom(typeInfo.Type))
                return;

            var hiddenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "version",
                "resourceName",
                "properties"
            };

            for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                if (hiddenProperties.Contains(typeInfo.Properties[i].Name))
                    typeInfo.Properties.RemoveAt(i);
            }
        });

        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, resolver);
    }
    
    public static IServiceCollection AddHttpModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}