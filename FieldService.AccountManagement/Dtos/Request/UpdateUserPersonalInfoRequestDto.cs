using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

public record UpdateUserPersonalInfoRequestDto(
    string? FullName,
    string? DisplayName,
    string? Email,
    string? Phone) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(UpdateUserPersonalInfoRequestDto);
    public override string? ResourceId => null;
}

