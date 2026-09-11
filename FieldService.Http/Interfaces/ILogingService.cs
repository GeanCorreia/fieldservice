using FieldService.Authentication.Entities;
using FieldService.Http.Dtos;

namespace FieldService.Http.Interfaces;

public interface ILoginService
{
    Task<Guid> LoginAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<UserAuthenticationDto?> GetUserTenantsAsync(
        CancellationToken cancellationToken = default);
    
    Task LogoutAsync(
        RevocationReason reason,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);
}