using FieldService.Shared.Types;

namespace FieldService.Shared.Dtos;

public record TenantDetail(
    Guid TenantId,
    string TenantName,
    Role Role,
    IReadOnlyList<Permission> Permissions,
    bool IsActive
    );

public record UserAuthentication(
    Guid Id,
    IReadOnlyCollection<TenantDetail> TenantDetails
);
