

using FieldService.Superset.Dtos;
using MediatR;

namespace FieldService.Superset.Cqrs.Queries.GetTenantSupersetUsers;

public record GetTenantSupersetUsersQuery(
    Guid TenantId,
    CancellationToken CancellationToken
    ) : IRequest<SupersetUserListResponseDto>;
