using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;
using FieldService.Shared.Types.Http;

namespace FieldService.AccountManagement.Cqrs.Queries.GetUsersByTeamId;

internal sealed class GetUsersByTeamIdHandler(IAccountManagementService service)
    : IRequestHandler<GetUsersByTeamIdQuery, IEnumerable<UserAccountDto>>
{
    public Task<IEnumerable<UserAccountDto>> Handle(
        GetUsersByTeamIdQuery request,
        CancellationToken cancellationToken) =>
        service.GetUsersByTeamIdAsync(request.TenantId, request.TeamId, cancellationToken);
}

internal sealed class GetUsersPageByTeamIdHandler(IAccountManagementService service)
    : IRequestHandler<GetUsersPageByTeamIdQuery, PaginatedResult<UserAccountDto>>
{
    public Task<PaginatedResult<UserAccountDto>> Handle(
        GetUsersPageByTeamIdQuery request, CancellationToken cancellationToken) =>
        service.GetUsersByTeamIdAsync(request.TenantId, request.TeamId, request.Page, cancellationToken);
}
