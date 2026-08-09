using FieldService.Shared.Types;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FieldService.Data.Interfaces;

public interface IEntityChangeExtractor
{
    IReadOnlyCollection<CollectedEntityChange> Extract(IReadOnlyCollection<EntityEntry> entries);
}
