using System.Security.Claims;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Services;

public sealed class DevelopmentEnvironmentUserIdentityResolver : IUserIdentityResolver
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IRequestContextManager _requestContextManager;
    private readonly ISessionAuthenticationService _sessionAuthenticationService;
    private readonly IUserAuthenticationMapper _userAuthenticationMapper;
    
    public DevelopmentEnvironmentUserIdentityResolver(
        IUserAuthenticationService userAuthenticationService,
        IRequestContextManager requestContextManager,
        ISessionAuthenticationService sessionAuthenticationService,
        IUserAuthenticationMapper userAuthenticationMapper)
    {
        _userAuthenticationService = userAuthenticationService ?? throw new ArgumentNullException(
            nameof(userAuthenticationService));
        _requestContextManager = requestContextManager ?? throw new ArgumentNullException(
            nameof(requestContextManager));
        _userAuthenticationMapper = userAuthenticationMapper ?? throw new ArgumentNullException(
            nameof(userAuthenticationMapper));
        _sessionAuthenticationService = sessionAuthenticationService ?? throw new ArgumentNullException(
            nameof(sessionAuthenticationService));
    }


    public async Task ResolveUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse("8961fdf5-f889-46a2-86a1-81bd35a876aa");
        var principal = _requestContextManager.Principal;
        var userCacheModel = await GetUserAuthenticationCache(userId);
        
        if (userCacheModel == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
        
        ClaimsResolver.UpsertClaim(identity, 
            ClaimsExtensions.UserId, 
            userCacheModel.UserId.ToString());
    }

    public Task ResolveSessionAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    private async Task<UserAuthenticationCacheModel?> GetUserAuthenticationCache(Guid userId, CancellationToken cancellationToken = default)
    {

        var user = await _userAuthenticationService.GetUserAsync(userId, cancellationToken);

        if (user == null)
        {
            return null;
        }
        return _userAuthenticationMapper.Map(user);
            
        
    }
}
