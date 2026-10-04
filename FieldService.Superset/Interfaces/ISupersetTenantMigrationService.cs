namespace FieldService.Superset.Interfaces;

public interface ISupersetTenantMigrationService
{
    Task MigrateTenantContainerAsync(
        Guid tenantId,
        Guid yamlMigrationFileId,
        CancellationToken cancellationToken = default
        );
}