namespace FieldService.Authentication.Types;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public AuthenticationSessionOptions Session { get; init; } = new();
}
