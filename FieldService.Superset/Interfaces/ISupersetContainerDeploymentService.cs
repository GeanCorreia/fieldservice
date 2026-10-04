using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetContainerDeploymentService
{
    Task<SupersetContainer> CreateInstanceAsync(
        Guid tenantId,
        SupersetTenantCreateParams supersetTenantCreateParams,
        Guid connectionStringId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}