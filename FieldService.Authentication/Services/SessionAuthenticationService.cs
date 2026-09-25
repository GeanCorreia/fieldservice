using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Services;

public class SessionAuthenticationService : ISessionAuthenticationService
{
    private readonly ISessionService _sessionService;
    private readonly ISessionRepository _sessionRepository;

    public SessionAuthenticationService(
        ISessionService sessionService,
        ISessionRepository sessionRepository)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task<Session?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionService.GetSessionAsync(
            sessionId,
            cancellationToken);

        
    }
}