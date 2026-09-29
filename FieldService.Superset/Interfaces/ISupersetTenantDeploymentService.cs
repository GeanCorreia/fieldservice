using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantDeploymentService
{
    Task CreateInstanceAsync(
        SupersetTenantCreateParams supersetTenantCreateParams,
        Guid connectionStringId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}