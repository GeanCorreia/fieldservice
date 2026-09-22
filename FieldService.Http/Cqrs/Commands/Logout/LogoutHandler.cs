using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Http.Cqrs.Commands;

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    
    private readonly ILogger<LogoutHandler> _logger;
    private readonly ISessionService _sessionService;
    private readonly ISessionMapper _sessionMapper;

    
    
    public LogoutHandler(
        ILogger<LogoutHandler> logger, 
        ISessionService sessionService,
        ISessionMapper sessionMapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
    }
    
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var sessionCacheModel = await _sessionService.GetSessionAsync(
                request.SessionId);
            
            if (sessionCacheModel == null)
            {
                throw new InvalidOperationException("Session not found.");
            }
            
            var session = _sessionMapper.Map(sessionCacheModel);
            session.Revoke(DateTimeOffset.UtcNow, RevocationReason.Logout );
            await _sessionService.SaveSessionAsync(sessionCacheModel, cancellationToken);
            
            
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the LogoutCommand.");
            throw;
        }
    }
}