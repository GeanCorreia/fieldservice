using FieldService.Shared.Types;

namespace FieldService.Authorization.Dtos;

public record TenantAuthorizationDto(
    Guid TenantId,
    Role Role,
    IReadOnlyCollection<Permission> Permissions,
    bool IsActive);

public sealed record UserAuthorizationDto
(
    Guid UserId,
    IEnumerable<TenantAuthorizationDto> Tenants,
    DateTimeOffset GeneratedAt
);
 