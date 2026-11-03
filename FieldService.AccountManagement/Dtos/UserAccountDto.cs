using FieldService.AccountManagement.Entities;

namespace FieldService.AccountManagement.Dtos;

internal record UserAccountDto(
    Guid Id,
    Guid TenantId,
    Guid TeamId,
    string TeamName,
    PersonalInfoDto PersonalInfo,
    JobTitleDto? JobTitle,
    bool IsDeactivated = false);


internal static class UserAccountDtoExtensions
{
    public static UserAccountDto ToDto(this UserAccount userAccount)
    {
        return new UserAccountDto(
            userAccount.Id,
            userAccount.TenantId,
            userAccount.TeamId,
            userAccount.TeamName,
            userAccount.PersonalInfo.ToDto(),
            userAccount.JobTitle?.ToDto(),
            userAccount.IsDeactivate());
    }
}    