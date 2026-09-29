using FieldService.Superset.Dtos;
using MediatR;

namespace FieldService.Superset.Cqrs.Queries.GetSupersetUser;

public record GetSupersetUserQuery(
    Guid UserId,
    Guid TenantId) : IRequest<SupersetUserDetailDto?>;
