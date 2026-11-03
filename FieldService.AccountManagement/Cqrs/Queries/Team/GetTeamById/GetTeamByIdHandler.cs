using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTeamById;

internal sealed class GetTeamByIdHandler(IAccountManagementService service)
    : IRequestHandler<GetTeamByIdQuery, TeamDto?>
{
    public Task<TeamDto?> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken) =>
        service.GetTeamByIdAsync(request.TenantId, request.TeamId, cancellationToken);
}

