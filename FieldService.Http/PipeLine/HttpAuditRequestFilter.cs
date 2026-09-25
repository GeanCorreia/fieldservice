using FieldService.Audit.Channels;
using FieldService.Audit.Entities;
using FieldService.Observability.Types;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Http.PipeLine;

public class HttpAuditRequestFilter(
    AuditRequestChannel channel
) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();
        var exception = executedContext.Exception;
        var auditRequest = BuildAuditRequest(executedContext.HttpContext, exception);
        await channel.EnqueueAsync(auditRequest, executedContext.HttpContext.RequestAborted);
    }

    private static AuditRequest BuildAuditRequest(
        HttpContext httpContext, 
        Exception? exception)
    {
        var claimsPrincipal = httpContext.User;
        var jwtId = ClaimsResolver.GetJwtId(claimsPrincipal);


        var statusCode = exception is not null ? 500 : httpContext.Response.StatusCode;
        var successful = statusCode >= 200 && statusCode < 400;
        var resource = ResolveResource(httpContext);

        Guid? sessionId; 
        try
        {
            sessionId = ClaimsResolver.GetSessionId(claimsPrincipal);
        }
        catch
        {
            sessionId = null;
        }

        return new AuditRequest(
            id: ObservabilityExecutionContext.RequestId ?? throw new InvalidOperationException("RequestId is missing"),
            jwtId: jwtId,
            ipAddressHash: HashService.CreateHashSha256(ObservabilityExecutionContext.IpAddress),
            timestamp: ObservabilityExecutionContext.Timestamp,
            channel: RequestChannel.Http,
            userAgentHash: HashService.CreateHashSha256(ObservabilityExecutionContext.UserAgent),
            resource: resource,
            successful: successful,
            statusCode: statusCode,
            userId: ClaimsResolver.GetUserId(claimsPrincipal),
            sessionId: sessionId
        );
    }

    private static string ResolveResource(HttpContext httpContext)
    {
        var method = httpContext.Request.Method.ToUpperInvariant();
        
        var rawPath = httpContext.Request.Path.Value ?? "/";
    
        return $"{method} {rawPath}";
    }
}