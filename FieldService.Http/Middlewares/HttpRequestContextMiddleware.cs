using FieldService.Authentication.Interfaces;
using FieldService.Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FieldService.Http.Middlewares;

public sealed class HttpRequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public HttpRequestContextMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IRequestContextManager contextManager,
        IRequestContextAdapter<HttpContext> adapter)
    {
        try
        {
            var requestContext = adapter.Adapt(httpContext);
            contextManager.Initialize(requestContext);
            contextManager.SetPrincipal(httpContext.User);
            await _next(httpContext);
        }
        catch (ArgumentException ex) when (string.Equals(ex.ParamName, "token", StringComparison.Ordinal))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsync("Authorization token is required.");
        }
    }
}