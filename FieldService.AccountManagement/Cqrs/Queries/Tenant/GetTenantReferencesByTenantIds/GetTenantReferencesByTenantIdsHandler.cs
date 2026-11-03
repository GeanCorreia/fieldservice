using FieldService.AccountManagement.Interfaces;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTenantReferencesByTenantIds;

internal sealed class GetTenantReferencesByTenantIdsHandler(IAccountManagementService service)
    : IRequestHandler<GetTenantReferencesByTenantIdsQuery, IEnumerable<TenantReferenceDto>>
{
    public Task<IEnumerable<TenantReferenceDto>> Handle(
        GetTenantReferencesByTenantIdsQuery request,
        CancellationToken cancellationToken) =>
        service.GetTenantReferencesByTenantIdsAsync(request.TenantIds, cancellationToken);
}

