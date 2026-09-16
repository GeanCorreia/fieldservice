using Amazon.Runtime.Internal;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.Http.Cqrs.Queries.GetUserTenant;

public record GetUserTenantQuery(
    Guid UserId,
    Guid TenantId
) : IRequest<UserTenantDto?>;
