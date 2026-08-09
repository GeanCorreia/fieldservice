using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using Microsoft.AspNetCore.Authentication;

namespace FieldService.Authentication.Interfaces;

public interface IUserAuthenticationService
{
    Task<UserAuthentication?> GetUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    Task<UserAuthenticationCacheModel?> GetUserAsync(
        string sub,
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default);
    
    
    
    Task UpdateUserAsync(
        UserAuthentication userAuthentication,
        CancellationToken ct = default);
}