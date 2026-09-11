using FieldService.Shared.Types;

namespace FieldService.Authentication.Types;

public record UserTenantAuthenticationCacheModel(
    Guid UserId,
    Guid TenantId,
    string Name);