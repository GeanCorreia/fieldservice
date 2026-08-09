using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface ILoginService
{
    Task<Guid> LoginAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<UserTenantAuthenticationCacheModel>> GetUserTenantsAsync(
        CancellationToken cancellationToken = default);
    
    Task LogoutAsync(
        RevocationReason reason,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);
}