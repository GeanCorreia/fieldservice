using FieldService.Superset.Dtos;
using MediatR;

namespace FieldService.Superset.Cqrs.Queries;

internal record GetGuestTokenQuery(
    Guid UserId,
    Guid TenantId,
    SupersetResourceDto SupersetResourceDto
    ) : IRequest<SupersetTokenResponse>;