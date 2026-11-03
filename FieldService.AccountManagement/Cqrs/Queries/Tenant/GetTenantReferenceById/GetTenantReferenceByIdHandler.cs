using FieldService.AccountManagement.Interfaces;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTenantReferenceById;

internal sealed class GetTenantReferenceByIdHandler(IAccountManagementService service)
    : IRequestHandler<GetTenantReferenceByIdQuery, TenantReferenceDto?>
{
    public Task<TenantReferenceDto?> Handle(GetTenantReferenceByIdQuery request, CancellationToken cancellationToken) =>
        service.GetTenantReferenceByIdAsync(request.TenantId, cancellationToken);
}

