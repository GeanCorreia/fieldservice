using FieldService.Shared.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Http.PipeLine;

public sealed class HttpCoreMiddleware
{
    private readonly CorsMiddleware _corsMiddleware;

    public HttpCoreMiddleware(
        RequestDelegate next,
        IOptions<ApplicationAccountOptions> options,
        ICorsService corsService,
        ILoggerFactory loggerFactory)
    {
        var policy = new CorsPolicyBuilder()
            .WithOrigins(options.Value.AllowedFrameAncestors)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        _corsMiddleware = new CorsMiddleware(next, corsService, policy, loggerFactory);
    }

    public Task InvokeAsync(HttpContext httpContext, ICorsPolicyProvider policyProvider)
    {
        return _corsMiddleware.Invoke(httpContext, policyProvider);
    }
}