using FieldService.Shared.Types;
using FieldService.Cloud.Entities;

namespace FieldService.Superset.Dtos;


public record SupersetTenantCreateParams(
    CloudProvider CloudProvider,
    ContainerConfiguration configuration,
    Guid? DedicatedDbConnectionStringId = null
    );



internal sealed record SupersetContainerCreationResult(
    string ResourceId,
    string FqdnUrl
);

