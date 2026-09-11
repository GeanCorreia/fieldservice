using System.Security.Claims;

namespace FieldService.Authentication.Interfaces;

public interface ISessionResolver
{
    Task ResolveSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
