using FieldService.Shared.Interfaces;

namespace FieldService.DocumentSupportManagement.Controllers;

public class DsmApiVersion : IApiVersion
{
    public Shared.Types.SchemaVersion SchemaVersion { get; } = new Shared.Types.SchemaVersion(1, 1, 4);
}