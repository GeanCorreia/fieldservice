namespace FieldService.AccountManagement.Dtos.Request;

public sealed record TransferUserTeamRequestDto(Guid TeamId);
public sealed record ChangeUserJobTitleRequestDto(Guid JobTitleId);
public sealed record DeactivateTeamRequestDto(Guid? TransferTeam = null);
/// <param name="TransferJobTitle">Required when users are still assigned to the job title.</param>
public sealed record DeactivateJobTitleRequestDto(Guid? TransferJobTitle = null);
