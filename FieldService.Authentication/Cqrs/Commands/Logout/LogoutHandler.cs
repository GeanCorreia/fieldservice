using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Authentication.Cqrs.Commands;

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly ILogger<LogoutHandler> _logger;
    private readonly ILoginService  _loginService;
    
    
    public LogoutHandler(ILogger<LogoutHandler> logger, ILoginService loginService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
    }
    
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _loginService.LogoutAsync(
                reason: RevocationReason.Logout,
                cancellationToken: cancellationToken);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the LogoutCommand.");
            throw;
        }
    }
}