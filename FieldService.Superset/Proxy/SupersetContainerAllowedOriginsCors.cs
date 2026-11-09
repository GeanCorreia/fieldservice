using Azure.ResourceManager.AppContainers.Models;
using FieldService.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Proxy;

internal class SupersetContainerAllowedOriginsCors
{
    private readonly ApplicationAccountOptions _applicationAccountOptions;

    public SupersetContainerAllowedOriginsCors(IOptions<ApplicationAccountOptions> applicationAccountOptions)
    {
        _applicationAccountOptions = applicationAccountOptions.Value;
    }

    public List<ContainerAppEnvironmentVariable> Get()
    {
        var frameAncestors = string.Join(" ", _applicationAccountOptions.AllowedFrameAncestors);

        return new List<ContainerAppEnvironmentVariable>
        {
            new ContainerAppEnvironmentVariable 
            { 
                Name = "ALLOWED_FRAME_ANCESTORS", 
                Value = frameAncestors 
            },

        };
    }
}