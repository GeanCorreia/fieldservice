using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Logs;
using FieldService.Authentication.Types;
using FieldService.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FieldService.Authentication.Services;

public class SessionCleanupService : ISessionPersistenceService
{
    private readonly ISessionCacheService _cacheService;
    private readonly ISessionRepository _sessionRepository;
    private readonly TimeSpan _inactivityThreshold;
    private readonly ISessionMapper _sessionMapper;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(
        ISessionCacheService cacheService,
        ISessionRepository sessionRepository,
        ISessionMapper sessionMapper,
        AuthenticationOptions authenticationOptions,
        ILogger<SessionCleanupService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(authenticationOptions);
        _inactivityThreshold = TimeSpan.FromMinutes(authenticationOptions.Session.PersistenceInactivityThresholdInMinutes);
    }

    public async Task InactiveCleanupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
           

            var sessionsCacheModel = await _cacheService.GetInactiveCandidatesAsync(
                _inactivityThreshold,
                cancellationToken);

            if (!sessionsCacheModel.Any())
            {
              
                return;
            }

            var revokedAt = DateTimeService.GetNow();
            var sessions = new List<Session>();

            foreach (var sessionCacheModel in sessionsCacheModel)
            {
                var session = _sessionMapper.Map(sessionCacheModel);
                var isActive = session.IsActive();
                if (isActive)
                {
                    session.Revoke(revokedAt, RevocationReason.Inactivity);
                    sessions.Add(session);
                    await _cacheService.RemoveSessionAsync(session.Id, cancellationToken);
                }
            }

            if (sessions.Any())
            {
                await _sessionRepository.Save(sessions, cancellationToken);
   
            }
        }
        catch (Exception ex)
        {
            _logger.LogSessionPersistenceError(LogLevel.Error, ex.Message);
            throw;
        }
    }

    // public async Task ExpiredCleanupAsync(CancellationToken cancellationToken = default)
    // {
    //     var shouldExpire = session.ShouldExpires(revokedAt);
    //     if (shouldExpire)
    //     {
    //         session.Revoke(revokedAt, RevocationReason.Inactivity);
    //         sessions.Add(session);
    //         await _cacheService.RemoveSessionAsync(session.Id, cancellationToken);
    //     }
    // }
}
