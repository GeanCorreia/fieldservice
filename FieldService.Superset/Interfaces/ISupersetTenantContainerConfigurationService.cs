using FieldService.Superset.Entities;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetTenantContainerConfigurationService
{
    Task ApplyContainerConfigurationAsync(
        SupersetTenantConfig tenantConfig,
        CancellationToken cancellationToken = default);
}

