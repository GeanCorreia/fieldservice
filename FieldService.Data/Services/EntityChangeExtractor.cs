using System.Text.Json;
using FieldService.Data.Interfaces;
using FieldService.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FieldService.Data.Services;

internal sealed class EntityChangeExtractor : IEntityChangeExtractor
{
    public IReadOnlyCollection<CollectedEntityChange> Extract(IReadOnlyCollection<EntityEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(BuildCollectedChange)
            .ToList()
            .AsReadOnly();
    }

    private static CollectedEntityChange BuildCollectedChange(EntityEntry entry)
    {
        var properties = entry.State == EntityState.Modified
            ? entry.Properties.Where(p => p.IsModified)
            : entry.Properties;

        var items = properties
            .Select(p =>
            {
                var oldValue = entry.State == EntityState.Added
                    ? JsonSerializer.SerializeToElement((object?)null)
                    : JsonSerializer.SerializeToElement(p.OriginalValue);

                var newValue = entry.State == EntityState.Deleted
                    ? JsonSerializer.SerializeToElement((object?)null)
                    : JsonSerializer.SerializeToElement(p.CurrentValue);

                return new AuditChangeItem(p.Metadata.Name, oldValue, newValue);
            })
            .ToList()
            .AsReadOnly();

        return new CollectedEntityChange(
            Resource: entry.Entity.GetType().Name,
            ResourceId: GetResourceId(entry),
            Changes: items);
    }

    private static string GetResourceId(EntityEntry entry)
    {
        var keyValues = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => (p.CurrentValue ?? p.OriginalValue)?.ToString() ?? string.Empty);

        return string.Join(",", keyValues);
    }
}
