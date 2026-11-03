using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

public record UpdateUserRequestDto(
    string? FullName,
    string? DisplayName,
    string? Email,
    string? Phone,
    Guid? JobTitleId,
    string? JobTitleDescription) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 1, 0);
    public override string ResourceName => nameof(UpdateUserRequestDto);
    public override string? ResourceId => null;
}

