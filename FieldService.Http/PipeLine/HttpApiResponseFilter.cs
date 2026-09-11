using FieldService.Shared.Dtos;
using FieldService.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Http.PipeLine;

public sealed class HttpApiResponseFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var executedContext = await next();

        if (executedContext.Result is not ObjectResult { Value: { } value } objectResult)
            return;

        if (value is ApiResponse)
            return;

        if (!IsEnvelopeCandidate(value))
            return;

        var requestId = ObservabilityExecutionContext.RequestId ?? ResolveRequestId(context.HttpContext.TraceIdentifier);
        var occurredAt = ObservabilityExecutionContext.Timestamp;
        var pagination = TryGetPagination(value);

        objectResult.Value = new ApiResponse(
            requestId,
            occurredAt,
            value,
            pagination);
    }

    private static Guid ResolveRequestId(string traceIdentifier)
    {
        if (Guid.TryParse(traceIdentifier, out var requestId))
            return requestId;

        return Guid.NewGuid();
    }

    private static bool IsEnvelopeCandidate(object value)
    {
        if (value is AbstractDto)
            return true;

        var dtoType = value.GetType();
        return dtoType.IsGenericType && dtoType.GetGenericTypeDefinition() == typeof(PagedDto<>);
    }

    private static PaginationResponse? TryGetPagination(object data)
    {
        var dtoType = data.GetType();
        if (!dtoType.IsGenericType || dtoType.GetGenericTypeDefinition() != typeof(PagedDto<>))
            return null;

        return dtoType
            .GetProperty(nameof(PagedDto<AbstractDto>.Pagination))?
            .GetValue(data) as PaginationResponse;
    }
}
