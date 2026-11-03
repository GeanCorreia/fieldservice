using FieldService.AccountManagement.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

internal static class AccountManagementResponseMappings
{
    public static TenantReferenceResponseDto ToResponse(this TenantReferenceDto dto) => new(dto.Id, dto.Name);

    public static TeamResponseDto ToResponse(this TeamDto dto) =>
        new(dto.Id, dto.Name, dto.Description, dto.LeaderId, dto.LeaderName, dto.IsDeactivated);

    public static JobTitleResponseDto ToResponse(this JobTitleDto dto) =>
        new(dto.Id, dto.Title, dto.Description, dto.IsDeactivated);

    public static PersonalInfoResponseDto ToResponse(this PersonalInfoDto dto) =>
        new(dto.FullName, dto.DisplayName, dto.Email, dto.Phone);

    public static UserAccountResponseDto ToResponse(this UserAccountDto dto) =>
        new(dto.Id, dto.TenantId, dto.TeamId, dto.TeamName, dto.PersonalInfo.ToResponse(), dto.JobTitle?.ToResponse(), dto.IsDeactivated);

    public static TenantResponseDto ToResponse(this TenantDto dto) =>
        new(
            dto.Tenant.ToResponse(),
            dto.JobTitles.Select(j => j.ToResponse()).ToList(),
            dto.Teams.Select(t => t.ToResponse()).ToList());
}

