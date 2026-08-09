using System.IdentityModel.Tokens.Jwt;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using System.Security.Claims;

namespace FieldService.Authentication.Services;

public class AzureUserIdentityResolver : IUserIdentityResolver
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IRequestContextManager _requestContextManager;
    private readonly ISessionAuthenticationService _sessionAuthenticationService;


    public AzureUserIdentityResolver(
        IUserAuthenticationService userAuthenticationService,
        IRequestContextManager requestContextManager,
        ISessionAuthenticationService sessionAuthenticationService )
    {
        _userAuthenticationService = userAuthenticationService ?? throw new ArgumentNullException(
            nameof(userAuthenticationService));
        _requestContextManager = requestContextManager ?? throw new ArgumentNullException(
            nameof(requestContextManager));
        _sessionAuthenticationService = sessionAuthenticationService ?? throw new ArgumentNullException(
            nameof(sessionAuthenticationService));
    }

    public async Task ResolveUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = _requestContextManager.Principal;
        var userCacheModel = await GetUserAuthenticationCache(principal);
        
        if (userCacheModel == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
        
        ClaimsResolver.UpsertClaim(identity, 
            ClaimsExtensions.UserId, 
            userCacheModel.UserId.ToString());
    }

    public async Task ResolveSessionAsync(
        CancellationToken cancellationToken = default)
    {
        var principal = _requestContextManager.Principal;
        var sessionId = _requestContextManager.Request.SessionId;
        if(sessionId == null)
        {
            throw new UnauthorizedAccessException("Session not found.");
        }
        
        var sessionCacheModel = await _sessionAuthenticationService.GetSessionAsync(
            sessionId.Value,
            cancellationToken);

        if (sessionCacheModel == null)
        {
            throw new UnauthorizedAccessException("Session not found.");
        }
        
        var userId = ClaimsResolver.GetUserId(principal);
        
        if (userId != sessionCacheModel.UserId)
        {
            throw new UnauthorizedAccessException("User not authorized for this session.");
        }
        
        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.SessionId, 
            sessionCacheModel.Id.ToString());
        ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.TenantId, 
            sessionCacheModel.TenantId.ToString());
        
        var context = _requestContextManager.Request;
        context.TenantId = sessionCacheModel.TenantId;
        
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
    
    

    
}