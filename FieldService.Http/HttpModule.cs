using FieldService.Http.Middlewares;
using FieldService.Http.Services;
using FieldService.Shared.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Http;

public static class HttpModule
{
    public static IServiceCollection AddHttpPipeline(this IServiceCollection services)
    {
        services.AddScoped<IRequestContextAdapter<HttpContext>, HttpRequestContextAdapter>();
        SwaggerModuleDiscovery.AddSwagger(services);
        return services;
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
        if (!environment.IsDevelopment())
            app.UseAuthentication();

        if (environment.IsDevelopment())
            app.UseMiddleware<DevelopmentHttpRequestContextMiddleware>();
        else
            app.UseMiddleware<HttpRequestContextMiddleware>();

        app.UseMiddleware<HttpSessionMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<HttpAuditMiddleware>();

        app.MapControllers();
        return app;
    }
}