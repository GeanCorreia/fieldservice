
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Logs;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace FieldService.Authentication.Services;

public class SessionManager : ISessionManager
{

    private readonly ISessionCacheService _sessionCacheService;
    private readonly ISessionMapper _sessionMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(
        ISessionMapper sessionMapper,
        ISessionCacheService sessionCacheService,
        IHttpContextAccessor httpContextAccessor,
        ISessionRepository sessionRepository,
        ILogger<SessionManager> logger)
    {

        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        _sessionCacheService = sessionCacheService ?? throw new ArgumentNullException(nameof(sessionCacheService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));

    }

    public async Task TouchAsync(
        CancellationToken cancellationToken = default)
    {
        var principal = GetHttpContext().User;
        var jwtId = ClaimsResolver.GetJwtId(principal);
        var sessionId =  ClaimsResolver.GetSessionId(principal);
        
        var jwtIdCached = await _sessionCacheService.GetSessionJwtIdAsync(
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
        
        var activity = CreateSessionActivity(sessionId, jwtId);
        
        var activityCacheModel = _sessionMapper.Map(activity);
        

        await _sessionCacheService.TouchSessionAsync(
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
            await _sessionCacheService.UpdateSessionJwtIdAsync(
                sessionId,
                jwtId,
                expiresAt,
                cancellationToken);

            var sessionCacheModel = await _sessionCacheService.GetSessionAsync(
                sessionId,
                cancellationToken);

            var session = _sessionMapper.Map(sessionCacheModel);
            session.UpdateExpiration(expiresAt);
            await _sessionRepository.Save(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogSessionUpdateExpirationError(LogLevel.Error, sessionId, ex.Message);
        }
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
    
    
    public static SessionActivity CreateSessionActivity(
        Guid sessionId,
        string jwtId)
    {
        var requestId = ObservabilityExecutionContext.RequestId ?? 
                        throw new InvalidOperationException("RequestId is inconsistent.");
        var ipAddress = string.IsNullOrWhiteSpace(ObservabilityExecutionContext.IpAddress)
            ? "unknown"
            : ObservabilityExecutionContext.IpAddress;
        
        return new SessionActivity(
            Guid.NewGuid(),
            sessionId: sessionId,
            jwtId: jwtId,
            ipAddressHash: HashService.GenerateHash(ipAddress),
            timestamp: ObservabilityExecutionContext.Timestamp,
            channel: ObservabilityExecutionContext.Channel,
            requestId: requestId,
            userAgentHash: string.IsNullOrWhiteSpace(ObservabilityExecutionContext.UserAgent)
                ? null
                : HashService.GenerateHash(ObservabilityExecutionContext.UserAgent)
        );
    }
}