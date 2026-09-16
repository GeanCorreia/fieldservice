using System.IdentityModel.Tokens.Jwt;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using System.Security.Claims;
using FieldService.Authentication.Dtos;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace FieldService.Authentication.Services;

internal class AzureUserIdentityResolver : IUserIdentityResolver
{
    private readonly IInternalUserAuthenticationService _internalUserAuthenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const AuthenticationProvider _authenticationProvider = AuthenticationProvider.AzureAdB2C;


    public AzureUserIdentityResolver(
        IInternalUserAuthenticationService internalUserAuthenticationService,
        IHttpContextAccessor httpContextAccessor)    
    {
        _internalUserAuthenticationService = internalUserAuthenticationService ?? throw new ArgumentNullException(
            nameof(internalUserAuthenticationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task ResolveUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = GetHttpContext().User;
        var userCacheModel = await GetUserTenantsAuthenticationType(principal);
        
        if (userCacheModel == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(GetHttpContext().User);
        
        ClaimsResolver.UpsertClaim(identity, 
            ClaimsExtensions.UserId, 
            userCacheModel.UserId.ToString());
        
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.UserId, userCacheModel.UserId.ToString());
        ClaimsResolver.UpsertClaim(identity,ClaimsExtensions.Provider,_authenticationProvider.ToString() );
    }

    private async Task<UserAuthenticationDto?> GetUserTenantsAuthenticationType(ClaimsPrincipal principal)
    {
        
        var sub = ClaimsResolver.GetSubjectId(principal);

        var userAuthenticationTenants = await _internalUserAuthenticationService.GetUserAsync(
            sub,
            AuthenticationProvider.AzureAdB2C,
            CancellationToken.None);
        
        return userAuthenticationTenants;
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

}