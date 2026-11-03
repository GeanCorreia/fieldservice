namespace FieldService.Shared.Types;

public record UserAuthorizationTenantDto(
    Guid TenantId,
    string TenantName,
    Role Role,
    IReadOnlyList<Permission> Permissions,
    bool IsActive
    );

public record UserTenantDto(
    Guid Id,
    UserAuthorizationTenantDto TenantDto
);

public record UserDto(
    Guid Id,
    IReadOnlyList<UserAuthorizationTenantDto> Tenants
);
