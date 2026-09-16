using FieldService.Authentication.Dtos;
using FieldService.Authentication.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Services;

internal class AuthenticationService : IAuthenticationService
{
    private readonly IInternalUserAuthenticationService _internalUserAuthenticationService;
    public AuthenticationService(
        IInternalUserAuthenticationService internalUserAuthenticationService)
    {
        _internalUserAuthenticationService = internalUserAuthenticationService ?? 
                                             throw new ArgumentNullException(nameof(internalUserAuthenticationService));
    }


    public async Task<UserAuthenticationDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _internalUserAuthenticationService.GetUserAsync(userId, cancellationToken);
    }
}