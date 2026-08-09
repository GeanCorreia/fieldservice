using System.Diagnostics;
using FieldService.Authentication.Services;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.Http.Services;

public sealed class HttpRequestContextAdapter 
    : IRequestContextAdapter<HttpContext>
{
    public RequestContext Adapt(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var traceId = Activity.Current?.TraceId.ToString();
        if (string.IsNullOrWhiteSpace(traceId))
        {
            throw new InvalidOperationException(
                "TraceId was not created. OpenTelemetry must be configured.");
        }
        
        var ipAddress = context.Connection.RemoteIpAddress?.ToString()
                        ?? throw new InvalidOperationException(
                            "Client IP address was not available.");

        var userAgent = context.Request.Headers.UserAgent
            .FirstOrDefault();
        
        var principal = context.User; 
        
        var sessionId = ResolveSessionId(context);

        if (sessionId != null)
        {
            var identiy = ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
            ClaimsResolver.UpsertClaim(identiy, ClaimsExtensions.SessionId, sessionId.Value.ToString());
        }
        
       
        
        return RequestContext.Create(
            RequestChannel.Http,
            ipAddress,
            traceId,
            userAgent,
            sessionId);
    }


    private static string ExtractToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authorization))
            return string.Empty;

        return authorization.StartsWith("Bearer ",
            StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : authorization.Trim();
    }
    
    private static Guid? ResolveSessionId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(
                "X-Session-Id",
                out var value))
        {
            return null;
        }

        return Guid.TryParse(value, out var sessionId)
            ? sessionId
            : null;
    }
}