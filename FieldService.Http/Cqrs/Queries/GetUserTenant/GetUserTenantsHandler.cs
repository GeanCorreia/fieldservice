using FieldService.Http.Dtos;
using FieldService.Http.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Http.Logs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Http.Cqrs.Queries;

public class GetUserTenantsHandler : IRequestHandler<GetUserTenantsQuery, UserAuthenticationDto?>
{
    private readonly ILogger<GetUserTenantsHandler> _logger;
    private readonly ILoginService _loginService;
    
    public GetUserTenantsHandler(ILogger<GetUserTenantsHandler> logger, ILoginService loginService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
    }
    
    public async Task<UserAuthenticationDto?> Handle(GetUserTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {

            var userDto = await _loginService.GetUserTenantsAsync(cancellationToken);
            
            if (userDto == null)
            {
                _logger.LogGetUserTenantsInconsistentUsers(LogLevel.Error);
                throw new UnauthorizedAccessException("User not found.");
            }

            return userDto;
        }
        catch(Exception ex)
        {
            _logger.LogGetUserTenantsQuery(LogLevel.Error, ex.Message);
            throw;
        }
    }
}