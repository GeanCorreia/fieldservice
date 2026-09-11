using FieldService.Shared.Dtos;
using FieldService.Shared.Responses;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FieldService.Http.PipeLine;

public sealed class HttpAuditAccessFilter(IAuditAccessService auditAccessService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var executedContext = await next();

        if (!ShouldTrackAccess(httpContext))
            return;

        if (executedContext.Result is not ObjectResult { Value: { } value })
            return;

        if (value is not ApiResponse { Data: { } data })
            return;

        var auditableData = ResolveAuditableData(data);
        if (auditableData is null)
            return;

        var parameters = httpContext.Request.Query
            .Select(q => new AuditAccessParameter
            {
                Property = q.Key,
                Value = q.Value.ToString()
            })
            .ToArray();
        var resourceId = ExtractResourceIdFromData(data) ?? ExtractResourceId(httpContext);

        await auditAccessService.AuditAccess(
            auditableData.ResourceName,
            auditableData.Version,
            resourceId,
            parameters,
            httpContext.RequestAborted);
    }

    private static bool ShouldTrackAccess(HttpContext httpContext)
        => !httpContext.Request.Path.StartsWithSegments("/hangfire");

    private static Guid? ExtractResourceId(HttpContext httpContext)
    {
        if (httpContext.Request.RouteValues.TryGetValue("id", out var routeIdRaw) &&
            Guid.TryParse(routeIdRaw?.ToString(), out var routeId))
        {
            return routeId;
        }

        if (httpContext.Request.Query.TryGetValue("id", out var queryIdRaw) &&
            Guid.TryParse(queryIdRaw.ToString(), out var queryId))
        {
            return queryId;
        }

        var path = httpContext.Request.Path.ToString();
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 && Guid.TryParse(segments[^1], out var pathId))
            return pathId;

        return null;
    }

    private static Guid? ExtractResourceIdFromData(object data)
    {
        if (data is AbstractDto dto)
            return ExtractResourceIdFromDto(dto);

        var dataType = data.GetType();
        if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(PagedDto<>))
            return null;

        if (data is System.Collections.IEnumerable and not string)
            return null;

        return null;
    }

    private static Guid? ExtractResourceIdFromDto(AbstractDto dto)
    {
        var idProperty = dto.GetType().GetProperty("Id");
        if (idProperty is null)
            return null;

        var idValue = idProperty.GetValue(dto);
        return idValue switch
        {
            Guid value => value,
            string value when Guid.TryParse(value, out var parsed) => parsed,
            _ => null
        };
    }

    private static AbstractDto? ResolveAuditableData(object data)
    {
        if (data is AbstractDto dto)
            return dto;

        var dataType = data.GetType();
        if (!dataType.IsGenericType || dataType.GetGenericTypeDefinition() != typeof(PagedDto<>))
            throw new InvalidOperationException($"Unsupported ApiResponse.Data type for audit: '{dataType.FullName}'.");

        var items = dataType.GetProperty(nameof(PagedDto<AbstractDto>.Items))?.GetValue(data) as System.Collections.IEnumerable;
        if (items is null)
            return null;

        foreach (var item in items)
        {
            if (item is AbstractDto itemDto)
                return itemDto;

            throw new InvalidOperationException("PagedDto.Items must contain elements inheriting from AbstractDto.");
        }

        return null;
    }
}
