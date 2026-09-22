using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantDeploymentService
{
    Task<SupersetTenantConfigParams> CreateInstanceAsync(
        SupersetTenantCreateParams supersetTenantCreateParams, 
        CancellationToken cancellationToken);
}