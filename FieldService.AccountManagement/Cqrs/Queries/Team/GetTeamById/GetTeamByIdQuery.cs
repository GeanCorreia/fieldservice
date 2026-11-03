using FieldService.AccountManagement.Dtos;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTeamById;

internal sealed record GetTeamByIdQuery(Guid TenantId, Guid TeamId) : IRequest<TeamDto?>;

