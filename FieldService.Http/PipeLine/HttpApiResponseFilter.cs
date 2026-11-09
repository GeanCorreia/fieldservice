using System.Text.Json;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Http.PipeLine;

public sealed class HttpApiResponseFilter(IOptions<JsonOptions> jsonOptions) : IAsyncActionFilter
{
    private static readonly HashSet<string> InternalPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "resourceId",
        "isActive"
    };

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var executedContext = await next();

        if (executedContext.Result is not ObjectResult { Value: { } value } objectResult)
            return;

        if (value is not ApiResponse && IsEnvelopeCandidate(value))
        {
            var requestId = ObservabilityExecutionContext.RequestId ??
                            ResolveRequestId(context.HttpContext.TraceIdentifier);
            var occurredAt = ObservabilityExecutionContext.Timestamp;
            var pagination = TryGetPagination(value);

            objectResult.Value = new ApiResponse(
                requestId,
                occurredAt,
                value,
                pagination);
        }

        var responseValue = objectResult.Value!;
        var serializedResponse = JsonSerializer.SerializeToElement(
            responseValue,
            responseValue.GetType(),
            jsonOptions.Value.JsonSerializerOptions);

        if (!ContainsInternalProperties(serializedResponse))
            return;

        objectResult.Value = RemoveInternalProperties(serializedResponse);
        objectResult.DeclaredType = typeof(JsonElement);
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

    private static Pagination? TryGetPagination(object data)
    {
        var dtoType = data.GetType();
        if (!dtoType.IsGenericType || dtoType.GetGenericTypeDefinition() != typeof(PagedDto<>))
            return null;

        return dtoType
            .GetProperty(nameof(PagedDto<AbstractDto>.Pagination))?
            .GetValue(data) as Pagination;
    }

    private static bool ContainsInternalProperties(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Any(property =>
                InternalPropertyNames.Contains(property.Name) || ContainsInternalProperties(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Any(ContainsInternalProperties),
            _ => false
        };
    }

    private static JsonElement RemoveInternalProperties(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            WriteWithoutInternalProperties(element, writer);

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static void WriteWithoutInternalProperties(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (InternalPropertyNames.Contains(property.Name))
                        continue;

                    writer.WritePropertyName(property.Name);
                    WriteWithoutInternalProperties(property.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteWithoutInternalProperties(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}