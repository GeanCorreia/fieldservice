using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTeamsByTenantId;

internal sealed class GetTeamsByTenantIdHandler(IAccountManagementService service)
    : IRequestHandler<GetTeamsByTenantIdQuery, IEnumerable<TeamDto>>
{
    public Task<IEnumerable<TeamDto>> Handle(GetTeamsByTenantIdQuery request, CancellationToken cancellationToken) =>
        service.GetTeamsByTenantIdAsync(request.TenantId, cancellationToken);
}

