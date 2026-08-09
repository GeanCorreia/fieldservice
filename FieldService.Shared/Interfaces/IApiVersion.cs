using FieldService.Shared.Types;

namespace FieldService.Shared.Interfaces;

public interface IApiVersion
{
    SchemaVersion SchemaVersion { get; }
}