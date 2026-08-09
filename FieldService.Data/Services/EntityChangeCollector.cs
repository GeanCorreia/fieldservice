using FieldService.Data.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Data.Services;

internal sealed class EntityChangeCollector : IEntityChangeCollector
{
    private readonly List<CollectedEntityChange> _changes = [];

    public void Collect(IReadOnlyCollection<CollectedEntityChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        foreach (var change in changes)
            Merge(_changes, change);
    }

    public IReadOnlyCollection<CollectedEntityChange> Consume()
    {
        var items = _changes.ToList().AsReadOnly();
        _changes.Clear();
        return items;
    }

    public void Clear() => _changes.Clear();

    private static void Merge(List<CollectedEntityChange> target, CollectedEntityChange incoming)
    {
        var index = target.FindIndex(x =>
            x.Resource == incoming.Resource &&
            x.ResourceId == incoming.ResourceId);

        if (index < 0)
        {
            target.Add(incoming);
            return;
        }

        var existing = target[index];
        var mergedItems = existing.Changes
            .Concat(incoming.Changes)
            .ToList()
            .AsReadOnly();

        target[index] = existing with { Changes = mergedItems };
    }
}
