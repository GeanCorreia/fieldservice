using FieldService.Shared.Types;

namespace FieldService.Shared.Dtos;

public record UserAuthorizationContextDto(
    Guid UserId,
    Guid TenantId,
    Role Role,
    IReadOnlyCollection<Permission> Permissions);

