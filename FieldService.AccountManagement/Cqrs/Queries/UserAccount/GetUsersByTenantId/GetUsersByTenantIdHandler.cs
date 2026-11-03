using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;
using FieldService.Shared.Types.Http;

namespace FieldService.AccountManagement.Cqrs.Queries.GetUsersByTenantId;

internal sealed class GetUsersByTenantIdHandler(IAccountManagementService service)
    : IRequestHandler<GetUsersByTenantIdQuery, IEnumerable<UserAccountDto>>
{
    public Task<IEnumerable<UserAccountDto>> Handle(
        GetUsersByTenantIdQuery request,
        CancellationToken cancellationToken) =>
        service.GetUsersByTenantIdAsync(request.TenantId, cancellationToken);
}

internal sealed class GetUsersPageByTenantIdHandler(IAccountManagementService service)
    : IRequestHandler<GetUsersPageByTenantIdQuery, PaginatedResult<UserAccountDto>>
{
    public Task<PaginatedResult<UserAccountDto>> Handle(
        GetUsersPageByTenantIdQuery request, CancellationToken cancellationToken) =>
        service.GetUsersByTenantIdAsync(request.TenantId, request.Page, cancellationToken);
}
