using FieldService.Authentication.Interfaces;
using FieldService.Authentication.SessionAttribute;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.Http.PipeLine;

public sealed class HttpAuthenticationMiddleware(RequestDelegate next)
{
    private const string SessionIdHeaderName = "X-Session-Id";
    private const string SessionIdQueryStringName = "sessionId";

    public async Task InvokeAsync(
        HttpContext httpContext,
        IUserIdentityResolver userIdentityResolver,
        ISessionResolver sessionResolver)
    {
        if (httpContext.GetEndpoint() is null) 
        {
            await next(httpContext);
            return;
        }

        await userIdentityResolver.ResolveUserAsync(httpContext.RequestAborted);
        
        if(ShouldSkipAuthentication(httpContext))
        {
            await next(httpContext);
            return;
        }

        var sessionAttribute = httpContext.GetEndpoint()!.Metadata.GetMetadata<ISessionOperation>();
        switch (sessionAttribute)
        {
            case SessionAtributes.TenantSelectionAttribute:
                await next(httpContext);
                break;
            case SessionAtributes.SessionCreationAttribute:
                await next(httpContext);
                break;
            
            case SessionAtributes.SignalRAttribute:
                await userIdentityResolver.ResolveUserAsync(httpContext.RequestAborted);
                InjectSessionFromQueryString(httpContext);
                await sessionResolver.ResolveSessionAsync(httpContext.User, httpContext.RequestAborted);
                await next(httpContext);
                break;
                
            default:
                InjectSessionIdFromHeader(httpContext);
                await sessionResolver.ResolveSessionAsync(httpContext.User, httpContext.RequestAborted);
                await next(httpContext);
                break;
        }
    }

    private static bool ShouldSkipAuthentication(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase);
    }

    private static void InjectSessionIdFromHeader(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(SessionIdHeaderName, out var values))
            return;

        if (!Guid.TryParse(values.FirstOrDefault(), out var sessionId))
            return;

        var principal = httpContext.User;
        var existing = principal.FindFirst(ClaimsExtensions.SessionId);
        if (existing is not null)
            return;

        var identity = Shared.Services.ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
        Shared.Services.ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.SessionId, sessionId.ToString());
    }
    
    private static void InjectSessionFromQueryString(HttpContext httpContext)
    {
        if (!httpContext.Request.Query.TryGetValue(SessionIdQueryStringName, out var values))
            return;

        if (!Guid.TryParse(values.FirstOrDefault(), out var sessionId))
            return;

        var principal = httpContext.User;
        var existing = principal.FindFirst(ClaimsExtensions.SessionId);
        if (existing is not null)
            return;

        var identity = Shared.Services.ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
        Shared.Services.ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.SessionId, sessionId.ToString());
    }
    
}
