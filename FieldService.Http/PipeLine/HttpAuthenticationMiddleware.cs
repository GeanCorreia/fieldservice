using FieldService.Authentication.Interfaces;
using FieldService.Authentication.SessionAttribute;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;


namespace FieldService.Http.PipeLine;

public sealed class HttpAuthenticationMiddleware(RequestDelegate next)
{

    public async Task InvokeAsync(
        HttpContext httpContext,
        IUserIdentityResolver userIdentityResolver)
    {
        if (httpContext.GetEndpoint() is null)
        {
            await next(httpContext);
            return;
        }

        if (ShouldSkipAuthentication(httpContext))
        {
            await next(httpContext);
            return;
        }
        
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await next(httpContext);
            return;
        }

        await userIdentityResolver.ResolveUserAsync(httpContext.RequestAborted);

        await next(httpContext);
        
    }

    private static bool ShouldSkipAuthentication(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        return path.Contains("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase);
    }
}
