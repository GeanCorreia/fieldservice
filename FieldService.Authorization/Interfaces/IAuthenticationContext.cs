using System.Security.Claims;

namespace FieldService.Authorization.Interfaces;

public interface IAuthenticationContext
{
    ClaimsPrincipal User { get; }
    Guid? TenantId { get; }
    Guid? UserId { get; }
    IReadOnlyList<string> Roles { get; }
}
