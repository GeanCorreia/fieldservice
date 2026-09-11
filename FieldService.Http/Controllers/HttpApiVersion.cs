using FieldService.Shared.Interfaces;

namespace FieldService.Http.Controllers;

public class HttpApiVersion : IApiVersion
{
    public Shared.Types.SchemaVersion SchemaVersion { get; } = new Shared.Types.SchemaVersion(1, 1, 4);
}