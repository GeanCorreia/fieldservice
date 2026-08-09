using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Services;

namespace FieldService.Authentication.Services;

public class SessionPersistenceService : ISessionPersistenceService
{
    private readonly ISessionCacheService _cacheService;
    private readonly ISessionRepository _sessionRepository;
    private readonly TimeSpan _inactivityThreshold;
    private readonly ISessionMapper _sessionMapper;

    public SessionPersistenceService(
        ISessionCacheService cacheService, 
        ISessionRepository sessionRepository,
        ISessionMapper sessionMapper,
        AuthenticationOptions authenticationOptions)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        ArgumentNullException.ThrowIfNull(authenticationOptions);
        _inactivityThreshold = TimeSpan.FromMinutes(authenticationOptions.Session.PersistenceInactivityThresholdInMinutes);
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
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
        
        foreach(var sessionCacheModel in sessionsCacheModel)
        {
            var session = _sessionMapper.Map(sessionCacheModel);
            if (!session.IsRevoked || !session.IsActive(revokedAt))
            {
                session.Revoke(revokedAt, RevocationReason.Inactivity);
            }
            sessions.Add(session);
            await _cacheService.RemoveSessionAsync(session.Id, cancellationToken);
            
        }

        if (sessions.Any())
        {
            await _sessionRepository.Save(sessions, cancellationToken);
        }
    }
    
    
}