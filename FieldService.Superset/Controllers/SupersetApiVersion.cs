using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Superset.Controllers;

public class SupersetApiVersion : IApiVersion
{
    public SchemaVersion SchemaVersion  => new SchemaVersion(1, 0, 0);
}