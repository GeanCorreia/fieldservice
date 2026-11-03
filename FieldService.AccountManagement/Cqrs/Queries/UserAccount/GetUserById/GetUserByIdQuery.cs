using FieldService.AccountManagement.Dtos;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetUserById;

internal sealed record GetUserByIdQuery(Guid TenantId, Guid UserId) : IRequest<UserAccountDto?>;

