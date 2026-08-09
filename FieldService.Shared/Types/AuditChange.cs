using System.Text.Json;

namespace FieldService.Shared.Types;

public sealed record AuditChangeItem(
    string PropertyName,
    JsonElement OldValue,
    JsonElement NewValue
);

public sealed record CollectedEntityChange(
    string Resource,
    string ResourceId,
    IReadOnlyCollection<AuditChangeItem> Changes);