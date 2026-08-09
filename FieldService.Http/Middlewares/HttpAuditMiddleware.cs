using FieldService.Shared.Interfaces;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FieldService.Http.Middlewares;

public class HttpAuditMiddleware
{
    private readonly RequestDelegate _next;

    public HttpAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IAuditTracker auditTracker)
    {
        if(httpContext.Request.Method == HttpMethods.Get)
        {
            var resource = httpContext.Request.Path;
            var resourceId = ExtractIdFromPath(resource);
            var parameters = httpContext.Request.Query
                .Select(q => new AuditAccessParameter
                {
                    Property = q.Key,
                    Value = q.Value.ToString()
                });
            auditTracker.TrackAccess(
                resource, 
                resourceId, 
                parameters);
        }

        try
        {
            await _next(httpContext);
        }
        finally
        {
            await auditTracker.Persist(httpContext.RequestAborted);
        }
    }
    
    private Guid? ExtractIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 && Guid.TryParse(segments.Last(), out var id))
        {
            return id;
        }
        return null;
    }
}