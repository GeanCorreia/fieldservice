using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Http.Dtos;

public record TenantDetailDto(
    Guid TenantId,
    string TenantName,
    string Role,
    IReadOnlyList<string> Permissions,
    bool IsActive
);

public record UserAuthenticationDto(
    IReadOnlyCollection<TenantDetailDto> TenantDetails
    ) : AbstractDto
{
    public override SchemaVersion Version => new(2, 1, 0);
    public override string ResourceName => "UserAuthenticationDto";
}
