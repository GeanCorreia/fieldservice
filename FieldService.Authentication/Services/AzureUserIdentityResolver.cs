using System.IdentityModel.Tokens.Jwt;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FieldService.Authentication.Services;

public class AzureUserIdentityResolver : IUserIdentityResolver
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public AzureUserIdentityResolver(
        IUserAuthenticationService userAuthenticationService,
        IHttpContextAccessor httpContextAccessor)    
    {
        _userAuthenticationService = userAuthenticationService ?? throw new ArgumentNullException(
            nameof(userAuthenticationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task ResolveUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = GetHttpContext().User;
        var userCacheModel = await GetUserAuthenticationCache(principal);
        
        if (userCacheModel == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(GetHttpContext().User);
        
        ClaimsResolver.UpsertClaim(identity, 
            ClaimsExtensions.UserId, 
            userCacheModel.UserId.ToString());
    }

    private async Task<UserAuthenticationCacheModel?> GetUserAuthenticationCache(ClaimsPrincipal principal)
    {
        
        var sub = ClaimsResolver.GetSubjectId(principal);

        var userCacheModel = await _userAuthenticationService.GetUserAsync(
            sub,
            AuthenticationProvider.AzureAdB2C,
            CancellationToken.None);
        
        return userCacheModel;
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

}