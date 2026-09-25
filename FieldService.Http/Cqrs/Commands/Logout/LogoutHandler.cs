using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Http.Cqrs.Commands;

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    
    private readonly ILogger<LogoutHandler> _logger;
    private readonly ISessionService _sessionService;
    
    public LogoutHandler(
        ILogger<LogoutHandler> logger, 
        ISessionService sessionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }
    
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionService.GetSessionAsync(
                request.SessionId,
                cancellationToken);
            
            if (session == null)
            {
                throw new InvalidOperationException("Session not found.");
            }
            
            session.Revoke(DateTimeOffset.UtcNow, RevocationReason.Logout );
            await _sessionService.SaveSessionAsync(session, cancellationToken);
            
            
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the LogoutCommand.");
            throw;
        }
        
    }
}