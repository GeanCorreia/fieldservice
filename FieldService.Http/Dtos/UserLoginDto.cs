using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Http.Dtos;

public record UserLoginDto(
    IEnumerable<TenantDto> Tenants) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "UserLoginDto";
    public override bool IsActive => true;
}