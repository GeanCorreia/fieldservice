using FieldService.Superset.Entities;

namespace FieldService.Superset.Dtos;


public record SupersetTenantCreateParams(
    ProviderType ProviderType,
    SupersetContainerConfiguration configuration,
    Guid? DedicatedDbConnectionStringId = null
    );



internal sealed record SupersetContainerCreationResult(
    string ResourceId,
    string FqdnUrl
);


