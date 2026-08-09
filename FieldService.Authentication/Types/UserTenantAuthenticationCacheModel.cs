namespace FieldService.Authentication.Types;

public record UserTenantAuthenticationCacheModel(
    Guid TenantId,
    string Name);