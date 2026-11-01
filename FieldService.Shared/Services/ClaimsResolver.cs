using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldService.Shared.Types;

namespace FieldService.Shared.Services;

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
        if (IsAzureAdPrincipal(principal))
        {
            return GetAzureAdOid(principal);
        }

        var subjectId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new InvalidOperationException("Subject not found in claims.");
        }

        return subjectId;
    }

    private static string GetAzureAdOid(ClaimsPrincipal principal)
    {
        var oid = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                  ?? principal.FindFirst("oid")?.Value;

        if (string.IsNullOrWhiteSpace(oid))
        {
            throw new InvalidOperationException("Azure AD object identifier not found in claims.");
        }

        return oid;
    }

    private static bool IsAzureAdPrincipal(ClaimsPrincipal principal)
    {
        var issuer = principal.FindFirst(JwtRegisteredClaimNames.Iss)?.Value;

        return principal.HasClaim(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")
               || principal.HasClaim(c => c.Type == "oid")
               || principal.HasClaim(c => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid")
               || (!string.IsNullOrWhiteSpace(issuer)
                   && (issuer.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase)
                       || issuer.Contains("sts.windows.net", StringComparison.OrdinalIgnoreCase)));
    }

    public static string GetProvider(ClaimsPrincipal principal)
    {
        var provider = principal.FindFirst(ClaimsExtensions.Provider)?.Value;
        if (provider == null)
        {
            throw new InvalidOperationException("Provider not found in claims.");
        }
        return provider;
    }

    public static string GetJwtId(ClaimsPrincipal principal)
    {
        var jwtId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                    ?? principal.FindFirst("jti")?.Value
                    // Azure AD v1 tokens frequently expose 'uti' instead of 'jti'.
                    ?? principal.FindFirst("uti")?.Value;

        if (string.IsNullOrWhiteSpace(jwtId))
        {
            throw new InvalidOperationException("JWT not found.");
        }
        return jwtId;
    }

    public static DateTimeOffset GetExpiresAt(ClaimsPrincipal principal)
    {
        var exp = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (exp == null)
        {
            throw new InvalidOperationException("Exp claim not found.");
        }
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));
        
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

    public static Guid? GetOptionalGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}