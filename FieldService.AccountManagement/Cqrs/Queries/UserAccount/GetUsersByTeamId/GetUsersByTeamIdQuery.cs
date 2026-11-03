using FieldService.AccountManagement.Dtos;
using MediatR;
using FieldService.Shared.Types.Http;

namespace FieldService.AccountManagement.Cqrs.Queries.GetUsersByTeamId;

internal sealed record GetUsersByTeamIdQuery(Guid TenantId, Guid TeamId) : IRequest<IEnumerable<UserAccountDto>>;

internal sealed record GetUsersPageByTeamIdQuery(Guid TenantId, Guid TeamId, PagedRequest Page) : IRequest<PaginatedResult<UserAccountDto>>;
