using System.Text.Json;
using FieldService.Shared.Types;

namespace FiledService.Audit.Entities;


public sealed class AuditChange
{
    public Guid Id { get; init; }

    public Guid RequestId { get; init; }

    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public string ResourceId { get; init; } = default!;

    public string Resource { get; init; } = default!;
    
    private JsonDocument? _changedProperties;

    public IReadOnlyCollection<FieldService.Shared.Types.AuditChangeItem> ChangedProperties =>
        Parse(_changedProperties).ToList().AsReadOnly();


    private AuditChange()
    {
    }


    public static AuditChange Create(
        Guid requestId,
        Guid userId,
        Guid tenantId,
        DateTimeOffset occurredAt,
        string resource,
        string resourceId,
        IEnumerable<FieldService.Shared.Types.AuditChangeItem> changedProperties)
    {
        ArgumentNullException.ThrowIfNull(changedProperties);

        var groupedChanges = changedProperties
            .GroupBy(change => change.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var items = group.ToList();
                    return new
                    {
                        oldValue = items[0].OldValue,
                        newValue = items[^1].NewValue
                    };
                },
                StringComparer.Ordinal);

        return new AuditChange
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            UserId = userId,
            TenantId = tenantId,
            OccurredAt = occurredAt,
            Resource = resource,
            ResourceId = resourceId,
            _changedProperties = JsonSerializer.SerializeToDocument(groupedChanges)
        };
    }


    private static IEnumerable<FieldService.Shared.Types.AuditChangeItem> Parse(JsonDocument? jsonDocument)
    {
        if (jsonDocument == null)
            return Enumerable.Empty<FieldService.Shared.Types.AuditChangeItem>();

        var changes = new List<FieldService.Shared.Types.AuditChangeItem>();

        foreach (var property in jsonDocument.RootElement.EnumerateObject())
        {
            var oldValue = property.Value.GetProperty("oldValue");
            var newValue = property.Value.GetProperty("newValue");

            changes.Add(
                new FieldService.Shared.Types.AuditChangeItem(
                    property.Name,
                    oldValue,
                    newValue));
        }

        return changes;
    }
}