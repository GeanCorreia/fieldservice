using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.Authentication.Services;

internal sealed class DevelopmentEnvironmentUserIdentityResolver : IUserIdentityResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInternalUserAuthenticationService _internalUserAuthenticationService;
    private const string _sub = "development-user";
    private const string _mockedUser = "85015d33-b139-44ec-8b71-c4d6fcf3e32e";
    private const AuthenticationProvider _authenticationProvider = AuthenticationProvider.AzureAd;
    
    public DevelopmentEnvironmentUserIdentityResolver(
        IHttpContextAccessor httpContextAccessor,
        IInternalUserAuthenticationService internalUserAuthenticationService)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _internalUserAuthenticationService = internalUserAuthenticationService ?? throw new ArgumentNullException(
            nameof(internalUserAuthenticationService));
    }


    public async Task ResolveUserAsync(CancellationToken cancellationToken = default)
    {
        
        
        var httpContext = GetHttpContext();
        
        var existingIdentity = httpContext.User.Identity as ClaimsIdentity;
    
        var identity = existingIdentity ?? new ClaimsIdentity(authenticationType: "DevelopmentAuth");
        
        if (string.IsNullOrEmpty(identity.AuthenticationType))
        {
            identity = new ClaimsIdentity(
                httpContext.User.Claims, 
                authenticationType: "DevelopmentAuth", 
                nameType: ClaimTypes.NameIdentifier, 
                roleType: ClaimTypes.Role);
        }
        
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.UserId, _mockedUser);
        ClaimsResolver.UpsertClaim(identity,ClaimsExtensions.Provider,_authenticationProvider.ToString() );
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Sub, _sub );
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"));
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds().ToString());
        
        httpContext.User = new ClaimsPrincipal(identity);
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
}
