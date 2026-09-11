using System.Security.Claims;
using FieldService.Authentication.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Services;

public sealed class SessionResolver(
    ISessionAuthenticationService sessionAuthenticationService) : ISessionResolver
{
    public async Task ResolveSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sessionId = ClaimsResolver.GetSessionId(principal);

        var sessionCacheModel = await sessionAuthenticationService.GetSessionAsync(sessionId, cancellationToken);
        if (sessionCacheModel is null)
            throw new UnauthorizedAccessException("Session not found.");

        var userId = ClaimsResolver.GetUserId(principal);
        if (userId != sessionCacheModel.UserId)
            throw new UnauthorizedAccessException("User not authorized for this session.");

        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.SessionId, sessionCacheModel.Id.ToString());
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.TenantId, sessionCacheModel.TenantId.ToString());
    }
}
