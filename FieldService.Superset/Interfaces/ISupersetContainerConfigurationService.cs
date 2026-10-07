using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ICloudContainerAppScale
{
}
internal interface ISupersetContainerConfigurationService
{
    string CloudResourceId(Guid containerId);
    
    ICloudContainerAppScale CreateScaleConfiguration(
        SupersetContainerConfiguration containerConfig,
        CancellationToken cancellationToken = default);
    
    Task ApplyContainerConfigurationAsync(
        string resourceId,
        SupersetContainerConfiguration configuration,
        CancellationToken cancellationToken = default);
    
}

