using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldService.Authentication.Dtos;
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
    private const AuthenticationProvider _authenticationProvider = AuthenticationProvider.AzureAdB2C;
    
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
        
        var userCacheModel = await GetUserTenantsAuthenticationType();
    
        if (userCacheModel == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

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
        
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.UserId, userCacheModel.UserId.ToString());
        ClaimsResolver.UpsertClaim(identity,ClaimsExtensions.Provider,_authenticationProvider.ToString() );
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Sub, _sub );
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"));
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds().ToString());
        
        httpContext.User = new ClaimsPrincipal(identity);
    }

    private async Task<UserAuthenticationDto?> GetUserTenantsAuthenticationType()
    {
        

        var userAuthenticationTenants = await _internalUserAuthenticationService.GetUserAsync(
            _sub,
            _authenticationProvider,
            CancellationToken.None);
        
        return userAuthenticationTenants;
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
}
