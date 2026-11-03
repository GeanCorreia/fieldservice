using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

public record UserAccountResponseDto(
    Guid Id,
    Guid TenantId,
    Guid TeamId,
    string TeamName,
    PersonalInfoResponseDto PersonalInfo,
    JobTitleResponseDto? JobTitle,
    bool IsDeactivated = false) : AbstractDto
{
    public override bool IsActive => !IsDeactivated;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(UserAccountResponseDto);
    public override string? ResourceId => Id.ToString();
}
