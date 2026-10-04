using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetRoleServices
{
    Task<IEnumerable<SupersetRole>> GetSupersetRoles(
        Guid tenantId, 
        CancellationToken cancellationToken = default);
    
}