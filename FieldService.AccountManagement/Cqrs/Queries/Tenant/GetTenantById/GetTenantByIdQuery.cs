using FieldService.AccountManagement.Dtos;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTenantById;

internal sealed record GetTenantByIdQuery(Guid TenantId) : IRequest<TenantDto?>;

