namespace FieldService.Authentication.Types;

public sealed record UserAuthenticationCacheModel(
    Guid UserId,
    string ExternalId,
    AuthenticationProvider Provider,
    ICollection<UserTenantAuthenticationCacheModel> Tenants);
