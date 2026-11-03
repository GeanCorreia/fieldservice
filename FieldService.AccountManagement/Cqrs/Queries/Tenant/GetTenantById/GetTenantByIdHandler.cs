using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTenantById;

internal sealed class GetTenantByIdHandler(IAccountManagementService service)
    : IRequestHandler<GetTenantByIdQuery, TenantDto?>
{
    public Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken) =>
        service.GetTenantByIdAsync(request.TenantId, cancellationToken);
}

