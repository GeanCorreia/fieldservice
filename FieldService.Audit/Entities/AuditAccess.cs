using System.Text.Json;
using FieldService.Shared.Types;

namespace FiledService.Audit.Entities;

public record AuditAccessParameter
{
    public required string Property { get; init; }
    public required string Value { get; init; }
}

public sealed class AuditAccess
{
    public Guid Id { get; init; }
    public Guid RequestId { get; init; }
    public Guid UserId { get; init; }
    public Guid? TenantId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string ResourceName { get; init; } = default!;
    public SchemaVersion SchemaVersion { get; init; }
    public Guid? ResourceId { get; init; }

    private JsonDocument? _parameters;

    public IReadOnlyCollection<AuditAccessParameter> Parameters =>
        _parameters == null
            ? []
            : JsonSerializer.Deserialize<List<AuditAccessParameter>>(
                _parameters.RootElement.GetRawText()) ?? [];
    

    public static AuditAccess Create(
        Guid requestId,
        Guid userId,
        Guid? tenantId,
        DateTimeOffset occurredAt,
        string resourceName,
        SchemaVersion schemaVersion,
        Guid? resourceId = null,
        IEnumerable<AuditAccessParameter>? parameters = null)
    {
        return new AuditAccess
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            UserId = userId,
            TenantId = tenantId,
            OccurredAt = occurredAt,
            ResourceName = resourceName,
            SchemaVersion = schemaVersion,
            ResourceId = resourceId,
            _parameters = JsonSerializer.SerializeToDocument(parameters),
        };
    }
}
