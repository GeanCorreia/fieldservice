using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using FieldService.Cloud.Entities;

namespace FieldService.Cloud.Interfaces;

public interface IAzureContainerService
{
    string ResourceId(string resourceName);
    Task<ContainerAppResource> GetAsync(string resourceId, CancellationToken cancellationToken = default);
    Task<ContainerAppResource?> FindByNameAsync(string resourceName, CancellationToken cancellationToken = default);
    Task<ContainerAppResource> CreateAsync(
        string resourceName,
        ContainerAppContainer container,
        ContainerAppScale scale,
        int targetPort,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(ContainerAppResource container, CancellationToken cancellationToken = default);
    Task<string> ResolveFqdnUrlAsync(
        ContainerAppResource container,
        int maxAttempts,
        TimeSpan delay,
        CancellationToken cancellationToken = default);
    Task<ContainerInstanceStatus> GetStatusAsync(string resourceId, CancellationToken cancellationToken = default);
    Task ApplyExecutionScaleAsync(
        string resourceId,
        ExecutionType executionType,
        bool isScaleUp,
        CancellationToken cancellationToken = default);
}
