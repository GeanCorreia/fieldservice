using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateTeam;

internal sealed record CreateTeamCommand(
    Guid TenantId,
    Guid TeamId,
    string Name,
    string? Description,
    Guid? LeaderId,
    Guid CreatedBy) : IRequest;

