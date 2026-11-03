using FieldService.AccountManagement.Dtos;
using MediatR;
using FieldService.Shared.Types.Http;

namespace FieldService.AccountManagement.Cqrs.Queries.GetUsersByTenantId;

internal sealed record GetUsersByTenantIdQuery(Guid TenantId) : IRequest<IEnumerable<UserAccountDto>>;

internal sealed record GetUsersPageByTenantIdQuery(Guid TenantId, PagedRequest Page) : IRequest<PaginatedResult<UserAccountDto>>;
