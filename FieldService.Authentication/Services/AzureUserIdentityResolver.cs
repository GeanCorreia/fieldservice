using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace FieldService.Authentication.Services;

internal class AzureUserIdentityResolver : IUserIdentityResolver
{
    private readonly IInternalUserAuthenticationService _internalUserAuthenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const AuthenticationProvider _authenticationProvider = AuthenticationProvider.AzureAd;


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
        var sub = ClaimsResolver.GetSubjectId(principal);
        var userId = await _internalUserAuthenticationService.GetUserIdAsync(
            sub,
            _authenticationProvider,
            cancellationToken);
        
        
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(GetHttpContext().User);
        
        ClaimsResolver.UpsertClaim(identity, 
            ClaimsExtensions.UserId, 
            userId.Value.ToString());
        
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.UserId, userId.Value.ToString());
        ClaimsResolver.UpsertClaim(identity,ClaimsExtensions.Provider,_authenticationProvider.ToString() );
    }

    
    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

}