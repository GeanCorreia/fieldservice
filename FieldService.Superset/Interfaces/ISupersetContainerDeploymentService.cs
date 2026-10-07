using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal readonly record struct SupersetContainerDeploymentResult(
    string ResourceId,
    string FqdnUrl);

internal interface ISupersetContainerDeploymentService
{
    Task<SupersetContainerDeploymentResult> CreateInstanceAsync(
        Guid tenantId,
        SupersetContainerConfiguration configuration,
        Guid connectionStringId,
        Guid secretKeyId,
        CancellationToken cancellationToken = default);
}