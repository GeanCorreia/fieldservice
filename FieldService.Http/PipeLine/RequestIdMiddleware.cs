using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Http.PipeLine;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-ID";

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var requestId = ResolveRequestId(httpContext);
        httpContext.TraceIdentifier = requestId.ToString();
        httpContext.Response.Headers[HeaderName] = requestId.ToString();
        ObservabilityExecutionContext.RequestId = requestId;

        await next(httpContext);
    }

    private static Guid ResolveRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
            Guid.TryParse(headerValue, out var parsedGuid) &&
            parsedGuid != Guid.Empty)
        {
            return parsedGuid;
        }

        return Guid.NewGuid();
    }
}