using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TenantId,
    Guid TeamId,
    Guid UpdatedBy,
    string? Name,
    string? Description,
    Guid? LeaderId) : IRequest;

