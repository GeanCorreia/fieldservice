using FieldService.Shared.Types;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Dtos;


public record SupersetTenantCreateParams(
    CloudProvider CloudProvider,
    SupersetContainerConfiguration configuration,
    Guid? DedicatedDbConnectionStringId = null
    );



internal sealed record SupersetContainerCreationResult(
    string ResourceId,
    string FqdnUrl
);


