using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Services;

public class SessionAuthenticationService : ISessionAuthenticationService
{
    private readonly ISessionService _sessionService;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionMapper _sessionMapper;

    public SessionAuthenticationService(
        ISessionService sessionService,
        ISessionRepository sessionRepository,
        ISessionMapper sessionMapper)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
    }

    public async Task<SessionCacheModel?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionService.GetSessionAsync(
            sessionId,
            cancellationToken);

        
    }
}