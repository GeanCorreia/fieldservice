using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.RemoveTeamLeader;

internal sealed record RemoveTeamLeaderCommand(
    Guid TenantId,
    Guid TeamId,
    Guid UpdatedBy) : IRequest;
