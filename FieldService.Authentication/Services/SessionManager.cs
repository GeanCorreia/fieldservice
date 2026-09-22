
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Logs;
using FieldService.Observability.Types;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace FieldService.Authentication.Services;

public class SessionManager : ISessionManager
{

    private readonly ISessionService _sessionService;
    private readonly ISessionMapper _sessionMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(
        ISessionMapper sessionMapper,
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor,
        ISessionRepository sessionRepository,
        ILogger<SessionManager> logger)
    {

        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));

    }

    public async Task TouchAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var principal = httpContext.User ?? throw new UnauthorizedAccessException("User not found.");
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
        
        var activity = CreateSessionActivity(httpContext);
        
        var activityCacheModel = _sessionMapper.Map(activity);
        

        await _sessionService.TouchSessionAsync(
            sessionId, 
            activityCacheModel, 
            cancellationToken);
        
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

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
    
    
    public static SessionActivity CreateSessionActivity(
        HttpContext httpContext)
    {
        
        return new SessionActivity(
            Guid.NewGuid(),
            sessionId: ClaimsResolver.GetSessionId(httpContext.User),
            jwtId: ClaimsResolver.GetJwtId(httpContext.User),
            ipAddressHash: httpContext.Connection.RemoteIpAddress?.ToString() ?? 
                throw new UnauthorizedAccessException("IP Address inconsistent."),
            timestamp: ObservabilityExecutionContext.Timestamp,
            channel: RequestChannel.Http,
            requestId: ClaimsResolver.GetRequestId(httpContext.User),
            userAgentHash:  HashService.CreateHashSha256(httpContext.Request.Headers.UserAgent.ToString())
        );
    }
}