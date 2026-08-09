using System.Security.Claims;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using Amazon.Runtime;


namespace FieldService.Authentication.Services;

public class SessionManager : ISessionManager
{

    private readonly ISessionCacheService _sessionCacheService;
    private readonly ISessionMapper _sessionMapper;
    private readonly IRequestContextManager _requestContext;
    private readonly ILoginService  _loginService;

    public SessionManager(
        ISessionMapper sessionMapper,
        ISessionCacheService sessionCacheService,
        IRequestContextManager requestContext,
        ILoginService loginService)
    {

        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        _sessionCacheService = sessionCacheService ?? throw new ArgumentNullException(nameof(sessionCacheService));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));

    }

    public async Task TouchAsync(
        CancellationToken cancellationToken = default)
    {
        var principal = _requestContext.Principal;
        var jwtId = ClaimsResolver.GetJwtId(principal);
        var sessionId =  ClaimsResolver.GetSessionId(principal);
        
        var jwtIdCached = await _sessionCacheService.GetSessionJwtIdAsync(
            sessionId, 
            cancellationToken);
        
        if(jwtIdCached == null || jwtId != jwtIdCached)
        {
            var expiresAt = ClaimsResolver.GetExpiresAt(principal);
            await _sessionCacheService.UpdateSessionJwtIdAsync(
                sessionId, 
                jwtId,
                expiresAt,
                cancellationToken);
        }
        
        var activity = CreateSessionActivity(
            sessionId, 
            jwtId,
            _requestContext.Request);
        
        var activityCacheModel = _sessionMapper.Map(activity);
        

        await _sessionCacheService.TouchSessionAsync(
            sessionId, 
            activityCacheModel, 
            cancellationToken);
        
        
    }
    
    

    public static SessionActivity CreateSessionActivity(
        Guid sessionId, 
        string jwtId,
        RequestContext context)
    {
        return new SessionActivity(
            Guid.NewGuid(),
            sessionId: sessionId,
            jwtId: jwtId,
            ipAddressHash: context.IpAddressHash,
            timestamp: context.Timestamp,
            channel: context.Channel,
            requestId: context.RequestId,
            userAgentHash: context.UserAgentHash
        );
    }
}