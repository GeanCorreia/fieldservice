using FieldService.AccountManagement.Dtos;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTeamsByTenantId;

internal sealed record GetTeamsByTenantIdQuery(Guid TenantId) : IRequest<IEnumerable<TeamDto>>;

