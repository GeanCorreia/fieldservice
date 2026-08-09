using FieldService.Shared.Interfaces;

namespace FieldService.Authentication.Controllers;

public class AuthApiVersion : IApiVersion
{
    public Shared.Types.SchemaVersion SchemaVersion { get; } = new Shared.Types.SchemaVersion(1, 1, 4);
}