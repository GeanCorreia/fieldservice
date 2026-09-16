using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Http.Dtos;

public record UserAuthenticationDto(
    IEnumerable<TenantDto> UserAuthenticationType
    ) : AbstractDto
{
    public override SchemaVersion Version => new(2, 2, 0);
    public override string ResourceName => "UserAuthenticationDto";
    public override bool IsActive => false;
}
