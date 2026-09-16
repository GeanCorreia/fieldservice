using FieldService.Authentication.Dtos;

namespace FieldService.Authentication.Interfaces;

public interface IAuthenticationService
{
    
    Task<UserAuthenticationDto?> GetUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
}