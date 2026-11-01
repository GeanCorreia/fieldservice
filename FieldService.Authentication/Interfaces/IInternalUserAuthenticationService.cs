using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

internal interface IInternalUserAuthenticationService
{
    
    Task<Guid?> GetUserIdAsync(
        string sub,
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default);
    
}

