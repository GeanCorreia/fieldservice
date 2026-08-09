using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Authentication.Cqrs.Queries;

public class GetUserTenantsHandler : IRequestHandler<GetUserTenantsQuery, IEnumerable<UserTenantAuthenticationCacheModel>>
{
    private readonly ILogger<GetUserTenantsHandler> _logger;
    private readonly ILoginService _loginService;
    
    public GetUserTenantsHandler(ILogger<GetUserTenantsHandler> logger, ILoginService loginService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
    }
    
    public async Task<IEnumerable<UserTenantAuthenticationCacheModel>> Handle(GetUserTenantsQuery request, CancellationToken cancellationToken)
    {

        try
        {
            return await _loginService.GetUserTenantsAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the GetUserTenantQuery.");
            throw;
        }
    }
}