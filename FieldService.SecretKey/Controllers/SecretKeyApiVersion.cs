using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.SecretKey.Controllers;

public class SecretKeyApiVersion : IApiVersion
{
    public SchemaVersion SchemaVersion { get; } = new SchemaVersion(1, 0, 0);
}