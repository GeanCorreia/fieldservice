using FieldService.Shared.Types;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

public interface ISupersetAuthService
{
    Task<string> GetAdminToken(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
    Task<string> SupersetLogin(
        UserTenantDto user, 
        CancellationToken cancellationToken = default);
    
    Task<SupersetRoleDto?> GetTenantScopeRole(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
    Task CreateTenantScopeRole(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
}