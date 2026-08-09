using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FieldService.Http.Services;

internal sealed class SessionIdHeaderOperationFilter : IOperationFilter
{
    private const string SessionIdHeaderName = "X-Session-Id";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);

        operation.Parameters ??= new List<OpenApiParameter>();

        if (operation.Parameters.Any(p =>
                string.Equals(p.Name, SessionIdHeaderName, StringComparison.OrdinalIgnoreCase) &&
                p.In == ParameterLocation.Header))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = SessionIdHeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Session identifier used by HttpRequestContextAdapter.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });
    }
}
