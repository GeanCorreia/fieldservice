using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Http.PipeLine;

public sealed class HttpObservabilityMiddleware(
    RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            await next(httpContext);
            return;
        }

        var principal = httpContext.User;
        var requestId = ClaimsResolver.GetRequestId(principal);;
        httpContext.TraceIdentifier = requestId.ToString();
        
        ObservabilityExecutionContext.RequestId = requestId;
        ObservabilityExecutionContext.Timestamp = DateTimeOffset.UtcNow;
        ObservabilityExecutionContext.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        ObservabilityExecutionContext.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
        ObservabilityExecutionContext.UserId = ClaimsResolver.GetUserId(principal);
        ObservabilityExecutionContext.TenantId = ClaimsResolver.GetOptionalGuid(principal, ClaimsExtensions.TenantId);
        ObservabilityExecutionContext.SessionId = ClaimsResolver.GetOptionalGuid(principal, ClaimsExtensions.SessionId);

        await next(httpContext);
    }
}
