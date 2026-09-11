using System.Text.Json;
using FieldService.Shared.Types;

namespace FiledService.Audit.Entities;

public sealed class AuditDtoSchema
{
    public string ResourceName { get; init; } = default!;
    public string Version { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }

    private JsonDocument _properties = default!;

    public JsonElement Properties => _properties.RootElement.Clone();

    private AuditDtoSchema()
    {
    }

    public static AuditDtoSchema Create(
        string resourceName,
        SchemaVersion version,
        JsonElement properties,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(version);

        return new AuditDtoSchema
        {
            ResourceName = resourceName,
            Version = version.ToString(),
            CreatedAt = createdAt,
            _properties = JsonSerializer.SerializeToDocument(properties)
        };
    }
}
