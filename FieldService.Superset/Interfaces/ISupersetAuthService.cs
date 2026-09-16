using FieldService.Shared.Types;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;



internal interface ISupersetAuthService
{
    Task<string?> GetAdminToken(CancellationToken cancellationToken = default);
    Task<bool> IsInWhiteList(
        UserTenantDto user, 
        string supersetResourceId, 
        CancellationToken cancellationToken = default);
    
}