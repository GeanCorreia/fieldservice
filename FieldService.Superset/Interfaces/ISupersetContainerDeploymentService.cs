using FieldService.Cloud.Entities;

namespace FieldService.Superset.Interfaces;

internal readonly record struct SupersetContainerDeploymentResult(
    string ResourceId,
    string FqdnUrl);

internal interface ISupersetContainerDeploymentService
{
    Task<SupersetContainerDeploymentResult> CreateInstanceAsync(
        Guid tenantId,
        ContainerConfiguration configuration,
        Guid connectionStringId,
        Guid secretKeyId,
        CancellationToken cancellationToken = default);
}