using System.Security.Claims;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Logs;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace FieldService.Authentication.Services;

public class SessionManager : ISessionManager
{

    private readonly ISessionService _sessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SessionManager> _logger;
    

    public SessionManager(
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SessionManager> logger)
    {
        
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task TouchAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var jwtId = ClaimsResolver.GetJwtId(principal);
        var sessionId =  ClaimsResolver.GetSessionId(principal);
        var jwtIdCached = await _sessionService.GetSessionJwtIdAsync(
            sessionId, 
            cancellationToken);
        
        if(jwtIdCached == null || jwtId != jwtIdCached)
        {
            var expiresAt = ClaimsResolver.GetExpiresAt(principal);

            try
            {
                await UpdateSessionExpirationAsync(
                    sessionId,
                    jwtId,
                    expiresAt,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogSessionUpdateExpirationError(LogLevel.Error, sessionId, ex.Message);
            }

        }
        
    }
    
    private async Task UpdateSessionExpirationAsync(
        Guid sessionId,
        string jwtId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sessionService.UpdateSessionJwtIdAsync(
                sessionId,
                jwtId,
                expiresAt,
                cancellationToken);
            
        }
        catch (Exception ex)
        {
            _logger.LogSessionUpdateExpirationError(LogLevel.Error, sessionId, ex.Message);
        }
    }
    
}