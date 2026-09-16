using FieldService.Shared.Types;

namespace FieldService.Shared.Types;

public record TenantDto(
    Guid TenantId,
    string TenantName,
    Role Role,
    IReadOnlyList<Permission> Permissions,
    bool IsActive
    );

public record UserTenantDto(
    Guid Id,
    TenantDto TenantDto
);

public record UserDto(
    Guid Id,
    IReadOnlyList<TenantDto> Tenants
);
