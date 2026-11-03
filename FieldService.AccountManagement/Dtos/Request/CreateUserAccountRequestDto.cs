using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

public record CreateUserAccountRequestDto(
    Guid? UserId,
    Guid TeamId,
    string TeamName,
    Guid JobTitleId,
    string JobTitle,
    string? JobTitleDescription,
    string FullName,
    string DisplayName,
    string Email,
    string Phone) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(CreateUserAccountRequestDto);
    public override string? ResourceId => UserId?.ToString();
}

