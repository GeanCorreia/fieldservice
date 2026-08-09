using FieldService.Authentication.Types;
using MediatR;

namespace FieldService.Authentication.Cqrs;

public record GetUserTenantsQuery() : IRequest<IEnumerable<UserTenantAuthenticationCacheModel>>;