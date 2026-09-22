using FieldService.Shared.Types;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetAuthService
{
    Task<string> GetAdminToken(
        string fqdnUrl, 
        CancellationToken cancellationToken = default);
    
    
}