using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.Http.Middlewares;

public sealed class DevelopmentHttpRequestContextMiddleware
{

    private readonly RequestDelegate _next;

    public DevelopmentHttpRequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, IRequestContextManager contextManager)
    {
        var traceId = httpContext.TraceIdentifier;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
        var sessionId = ResolveSessionId(httpContext);

        var requestContext = RequestContext.Create(
            RequestChannel.Http,
            ipAddress,
            traceId,
            userAgent,
            sessionId);

        contextManager.Initialize(requestContext);

        var identity = new ClaimsIdentity(ClaimsExtensions.AuthenticationIdentity);
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "development-user"));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));
        identity.AddClaim(new Claim(
            JwtRegisteredClaimNames.Exp,
            DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds().ToString()));
        if (sessionId.HasValue)
        {
            identity.AddClaim(new Claim(ClaimsExtensions.SessionId, sessionId.Value.ToString()));
        }


        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        contextManager.SetPrincipal(principal);
        httpContext.Response.Headers["X-Development-Auth"] = "Mocked";

        await _next(httpContext);
    }

    private static Guid? ResolveSessionId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Session-Id", out var value))
            return null;

        return Guid.TryParse(value, out var sessionId)
            ? sessionId
            : null;
    }
}
