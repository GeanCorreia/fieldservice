using FieldService.Shared.Interfaces;

namespace FieldService.Form.Controllers;

public class FormApiVersion : IApiVersion
{
    public Shared.Types.SchemaVersion SchemaVersion { get; } = new Shared.Types.SchemaVersion(1, 1, 4);
}