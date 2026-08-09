using FieldService.Shared.Types;

namespace FieldService.Authorization.Types;

public sealed record UserAuthorizationSnapshot
(
    Guid UserId,
    Guid TenantId,
    Role Role,
    IReadOnlyCollection<Permission> Permissions,
    bool IsActive,
    DateTime GeneratedAt
);
 