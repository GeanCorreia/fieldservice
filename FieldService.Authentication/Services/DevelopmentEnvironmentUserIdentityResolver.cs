using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.Authentication.Services;

public sealed class DevelopmentEnvironmentUserIdentityResolver : IUserIdentityResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IUserAuthenticationMapper _userAuthenticationMapper;
    
    public DevelopmentEnvironmentUserIdentityResolver(
        IHttpContextAccessor httpContextAccessor,
        IUserAuthenticationService userAuthenticationService,
        IUserAuthenticationMapper userAuthenticationMapper)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userAuthenticationService = userAuthenticationService ?? throw new ArgumentNullException(
            nameof(userAuthenticationService));

        _userAuthenticationMapper = userAuthenticationMapper ?? throw new ArgumentNullException(
            nameof(userAuthenticationMapper));
    }


    public async Task ResolveUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse("8961fdf5-f889-46a2-86a1-81bd35a876aa");

        var userCacheModel = await GetUserAuthenticationCache(userId, cancellationToken);
    
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
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"));
        ClaimsResolver.UpsertClaim(identity, JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds().ToString());
        
        httpContext.User = new ClaimsPrincipal(identity);
    }

    private async Task<UserAuthenticationCacheModel?> GetUserAuthenticationCache(Guid userId, CancellationToken cancellationToken = default)
    {

        return await _userAuthenticationService.GetUserAsync(userId, cancellationToken);

        
            
        
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
}
