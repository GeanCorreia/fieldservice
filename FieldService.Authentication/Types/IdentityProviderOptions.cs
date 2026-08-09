namespace FieldService.Authentication.Types;

public sealed class IdentityProviderOptions
{
    public string Name { get; init; } = string.Empty;
    public string[] Issuers { get; init; } = [];
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string? AppIdUri { get; init; }
    public string[]? ValidAudiences { get; init; }
    public int ClockSkewSeconds { get; init; } = 60;
    public bool IsMultiTenant { get; init; } = false;
}
