using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateTeam;

internal sealed record DeactivateTeamCommand(
    Guid TenantId,
    Guid TeamId,
    Guid UpdatedBy,
    Guid? TransferTeam = null) : IRequest;
