using FieldService.Authentication.Dtos;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authentication;

namespace FieldService.Authentication.Interfaces;

internal interface IInternalUserAuthenticationService
{
    Task<UserAuthenticationDto?> GetUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    Task<UserAuthenticationDto?> GetUserAsync(
        string sub,
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default);
    
}

