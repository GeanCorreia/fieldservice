using System.Security.Claims;

namespace FieldService.Authorization.Interfaces;

public interface IAuthorizationService
{
    bool IsAuthorized(ClaimsPrincipal user, string permission);
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, string permission, CancellationToken ct = default);
    bool HasRole(ClaimsPrincipal user, string role);
    bool IsInTenant(ClaimsPrincipal user, Guid tenantId);
}
