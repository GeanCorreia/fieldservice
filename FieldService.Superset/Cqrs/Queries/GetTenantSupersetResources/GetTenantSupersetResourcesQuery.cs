using FieldService.Superset.Dtos;
using MediatR;

namespace FieldService.Superset.Cqrs.Queries.GetTenantSupersetResources;

public record GetTenantSupersetResourcesQuery(
    Guid UserId,
    Guid TenantId
) : IRequest<SupersetTenantResources>;