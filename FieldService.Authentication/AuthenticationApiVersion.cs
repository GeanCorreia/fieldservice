using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Authentication;

public sealed class AuthenticationApiVersion : IApiVersion
{
    public SchemaVersion SchemaVersion { get; } = new(1, 0, 0);
}
