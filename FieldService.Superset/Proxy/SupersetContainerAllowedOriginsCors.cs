using Azure.ResourceManager.AppContainers.Models;
using FieldService.Http.Configuration;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Proxy;

internal class SupersetContainerAllowedOriginsCors
{
    private readonly HttpOptions _httpOptions;

    public SupersetContainerAllowedOriginsCors(IOptions<HttpOptions> httpOptions)
    {
        _httpOptions = httpOptions.Value;
    }

    public List<ContainerAppEnvironmentVariable> Get()
    {
        var frameAncestors = string.Join(" ", _httpOptions.AllowedOrigins);

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