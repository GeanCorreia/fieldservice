using FieldService.Http.PipeLine;
using FieldService.Shared;
using FieldService.Shared.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.InfraTest.Http;

public sealed class HttpCoreMiddlewareTests
{
    [Theory]
    [InlineData("/api/v1/superset/dashboard", "http://localhost:5173", true)]
    [InlineData("/hubs/signalr/negotiate", "http://localhost:3000", true)]
    [InlineData("/api/v1/superset/dashboard", "https://untrusted.example", false)]
    [InlineData("/hubs/signalr/negotiate", "http://localhost:3001", false)]
    public async Task Preflight_uses_shared_frontend_origins(string path, string origin, bool allowed)
    {
        using var provider = CreateServices();
        var nextCalled = false;
        var middleware = CreateMiddleware(provider, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = origin;
        context.Request.Headers.AccessControlRequestMethod = HttpMethods.Post;
        context.Request.Headers.AccessControlRequestHeaders = "authorization,content-type";

        await middleware.InvokeAsync(context, provider.GetRequiredService<ICorsPolicyProvider>());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        if (allowed)
        {
            Assert.Equal(origin, context.Response.Headers.AccessControlAllowOrigin.ToString());
            Assert.Equal("true", context.Response.Headers.AccessControlAllowCredentials.ToString());
            Assert.Contains("POST", context.Response.Headers.AccessControlAllowMethods.ToString());
            Assert.Contains("authorization", context.Response.Headers.AccessControlAllowHeaders.ToString());
        }
        else
        {
            Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
            Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Credentials"));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost:5173")]
    [InlineData("https://untrusted.example")]
    public async Task Non_preflight_requests_continue_pipeline(string? origin)
    {
        using var provider = CreateServices();
        var nextCalled = false;
        var middleware = CreateMiddleware(provider, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        if (origin is not null)
            context.Request.Headers.Origin = origin;

        await middleware.InvokeAsync(context, provider.GetRequiredService<ICorsPolicyProvider>());

        Assert.True(nextCalled);
    }

    private static ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationAccount:AllowedFrameAncestors:0"] = "http://localhost:5173",
                ["ApplicationAccount:AllowedFrameAncestors:1"] = "http://localhost:3000"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedModule(configuration);
        services.AddCors();
        return services.BuildServiceProvider();
    }

    private static HttpCoreMiddleware CreateMiddleware(IServiceProvider provider, RequestDelegate next)
    {
        return new HttpCoreMiddleware(
            next,
            provider.GetRequiredService<IOptions<ApplicationAccountOptions>>(),
            provider.GetRequiredService<ICorsService>(),
            provider.GetRequiredService<ILoggerFactory>());
    }
}
