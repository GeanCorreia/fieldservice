using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Services;

public static class ClaimsResolver
{
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimsExtensions.UserId)?.Value;
        if (userId == null)
        {
            throw new InvalidOperationException("UserId not found in claims.");
        }
        return Guid.Parse(userId);
    }
    
    public static Guid GetTenantId(ClaimsPrincipal principal)
    {
        var tenantId = principal.FindFirst(ClaimsExtensions.TenantId)?.Value;
        if (tenantId == null)
        {
            throw new InvalidOperationException("TenantId not found in claims.");
        }
        return Guid.Parse(tenantId);
    }

    public static (Guid UserId, Guid TenantId) GetUserAndTenantId(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimsExtensions.UserId)?.Value;
        var tenantId = principal.FindFirst(ClaimsExtensions.TenantId)?.Value;

        if (userId == null || tenantId == null)
        {
            throw new InvalidOperationException("UserId or TenantId not found in claims.");
        }

        return (Guid.Parse(userId), Guid.Parse(tenantId));
    }
    
    public static Guid GetSessionId(ClaimsPrincipal principal)
    {
        var sessionId = principal.FindFirst(ClaimsExtensions.SessionId)?.Value;

        if (sessionId == null)
        {
            throw new InvalidOperationException("SessionId not found in claims.");
        }

        return Guid.Parse(sessionId);
    }

    public static string GetSubjectId(ClaimsPrincipal principal)
    {
        var subjectId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (subjectId == null)
        { 
            throw new InvalidOperationException("Subject not found in claims.");
        }
        return subjectId;
    }

    public static string GetJwtId(ClaimsPrincipal principal)
    {
        var jwtId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (jwtId == null)
        {
            throw new InvalidOperationException("JWT not found.");
        }
        return jwtId;
    }

    public static DateTime GetExpiresAt(ClaimsPrincipal principal)
    {
        var exp = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (exp == null)
        {
            throw new InvalidOperationException("Exp claim not found.");
        }
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;
        
    }
    
    public static ClaimsIdentity GetOrCreateAuthenticationIdentity(ClaimsPrincipal principal)
    {
        var identity = principal.Identities
            .OfType<ClaimsIdentity>()
            .FirstOrDefault(i => i.AuthenticationType == ClaimsExtensions.AuthenticationIdentity);

        if (identity != null)
            return identity;

        var newIdentity = new ClaimsIdentity(ClaimsExtensions.AuthenticationIdentity);
        principal.AddIdentity(newIdentity);
        return newIdentity;
    }

    public static void UpsertClaim(ClaimsIdentity identity, string claimType, string claimValue)
    {
        var currentClaims = identity.FindAll(claimType).ToList();
        foreach (var claim in currentClaims)
            identity.RemoveClaim(claim);

        identity.AddClaim(new Claim(claimType, claimValue));
    }
}