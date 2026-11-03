using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetUserById;

internal sealed class GetUserByIdHandler(IAccountManagementService service)
    : IRequestHandler<GetUserByIdQuery, UserAccountDto?>
{
    public Task<UserAccountDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) =>
        service.GetUserByIdAsync(request.TenantId, request.UserId, cancellationToken);
}

