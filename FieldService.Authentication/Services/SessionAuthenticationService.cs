using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Services;

public class SessionAuthenticationService : ISessionAuthenticationService
{
    private readonly ISessionCacheService _sessionCacheService;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionMapper _sessionMapper;

    public SessionAuthenticationService(
        ISessionCacheService sessionCacheService,
        ISessionRepository sessionRepository,
        ISessionMapper sessionMapper)
    {
        _sessionCacheService = sessionCacheService ?? throw new ArgumentNullException(nameof(sessionCacheService));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
    }

    public async Task<SessionCacheModel?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var sessionCacheModel = await _sessionCacheService.GetSessionAsync(
            sessionId,
            cancellationToken);

        if (sessionCacheModel != null)
        {
            return sessionCacheModel;
        }

        var session = await _sessionRepository.GetById(
            sessionId,
            cancellationToken);

        if (session == null)
        {
            return null;
        }

        sessionCacheModel = _sessionMapper.Map(session);
        await _sessionCacheService.SaveSessionAsync(
            sessionCacheModel,
            cancellationToken);

        return sessionCacheModel;
    }
}