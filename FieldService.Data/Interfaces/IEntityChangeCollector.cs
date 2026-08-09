using FieldService.Shared.Types;

namespace FieldService.Data.Interfaces;

public interface IEntityChangeCollector
{
    void Collect(IReadOnlyCollection<CollectedEntityChange> changes);
    IReadOnlyCollection<CollectedEntityChange> Consume();
    void Clear();
}
