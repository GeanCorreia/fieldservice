using FieldService.Authentication.Interfaces;
using FieldService.Http.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Http.Cqrs.Commands.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Guid>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly ILoginService _loginService;
    
    public LoginHandler(ILogger<LoginHandler> logger, ILoginService loginService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
    }

    public async Task<Guid> Handle(
        LoginCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = await _loginService.LoginAsync(request.TenantId, cancellationToken);
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing login for TenantId: {TenantId}", request.TenantId);
            throw;
        }
    }
}