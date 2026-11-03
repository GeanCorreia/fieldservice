using FieldService.Shared.Types;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Queries.GetTenantReferencesByTenantIds;

public sealed record GetTenantReferencesByTenantIdsQuery(IEnumerable<Guid> TenantIds)
    : IRequest<IEnumerable<TenantReferenceDto>>;

