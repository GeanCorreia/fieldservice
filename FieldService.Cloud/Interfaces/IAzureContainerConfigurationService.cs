using Azure.ResourceManager.AppContainers.Models;
using FieldService.Cloud.Entities;

namespace FieldService.Cloud.Interfaces;

public interface IAzureContainerConfigurationService
{
    ContainerAppScale CreateScaleConfiguration(ContainerConfiguration configuration, string cronTimezone);
    Task ApplyAsync(
        string resourceId,
        ContainerConfiguration configuration,
        string cronTimezone,
        CancellationToken cancellationToken = default);
}
