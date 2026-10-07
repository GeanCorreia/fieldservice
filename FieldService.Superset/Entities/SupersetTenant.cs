using System.ComponentModel.DataAnnotations.Schema;
using FiledService.Shared.Attributes;

namespace FieldService.Superset.Entities;

public enum SupersetTenantStatus
{
    Active = 1,
    Suspended = 2,
    Cancelled = 3
}

[Auditable("SupersetTenant", nameof(Id))]
internal class SupersetTenant
{
    
    public static string DatabasePrefix = "db_tenant";
    public static string DataSchemaPrefix = "superset_data";
    public static string MetadataSchemaPrefix = "superset";
    public static string MockedDataSchemaPrefix = "superset_mocked_data";
    public static string SupersetSecretKeyPrefix = "superset_secret_key";
    public static string ConnectionStringPrefix = "superset_connection_string";
    public static string Database(Guid tenantId) => $"{DatabasePrefix}_{tenantId.ToString().ToLower()}";
    public static string SecretKeyName(Guid tenantId) => $"{SupersetSecretKeyPrefix}_{tenantId.ToString().ToLower()}";
    public static string ConnectionStringName(Guid tenantId) => $"{ConnectionStringPrefix}_{tenantId.ToString().ToLower()}";
    
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid ContainerId { get; private set; }
    public SupersetContainer Container { get; private set; }
    public Guid ConnectionStringId { get; set; }
    public SupersetTenantStatus Status { get; private set; }
    
    [NotMapped]
    public string FqdnUrl => Container.FqdnUrl;
    public DateTimeOffset StatusUpdatedAt { get; private set; }
    [NotMapped] public string DatabaseAppName => $"{DatabasePrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string DataSchemaName => $"{DataSchemaPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string MetadataSchemaName => $"{MetadataSchemaPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string MockedDataSchemaName => $"{MockedDataSchemaPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string SupersetSecretKeyName => $"{SupersetSecretKeyPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string SupersetConnectionStringName => $"{ConnectionStringPrefix}_{TenantId.ToString().ToLower()}";

    protected SupersetTenant() { }

    private SupersetTenant(
        Guid id, 
        Guid tenantId, 
        SupersetContainer container, 
        Guid connectionStringId,
        DateTimeOffset statusUpdatedAt,
        SupersetTenantStatus status)
    {
        Id = id;
        TenantId = tenantId;
        ContainerId = container.Id;
        Container = container;
        ConnectionStringId = connectionStringId;
        StatusUpdatedAt = statusUpdatedAt;
        Status = status;

    }

    public static SupersetTenant Create(
        Guid tenantId, 
        SupersetContainer supersetContainer, 
        Guid connectionStringId)
    {
        return new SupersetTenant(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            container: supersetContainer,
            connectionStringId: connectionStringId,
            statusUpdatedAt: DateTimeOffset.UtcNow,
            status: SupersetTenantStatus.Active);
    }
    
    
    
    public void UpdateContainer(
        SupersetContainer supersetContainer)
    {
       Container = supersetContainer;
       ContainerId = supersetContainer.Id;
    }
    
    public void MarkAsSuspended()
    {
        if(Status != SupersetTenantStatus.Active)
        {
            throw new InvalidOperationException("Cannot suspend a tenant that is not active." +
                                                "Actual Status: " + Status.ToString());
        }
        Status = SupersetTenantStatus.Suspended;
        StatusUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsCancelled()
    {
        if(Status == SupersetTenantStatus.Cancelled)
        {
            throw new InvalidOperationException("Tenant is already cancelled.");
        }
        Status = SupersetTenantStatus.Cancelled;
        StatusUpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void MarkAsActive()
    {
        if(Status != SupersetTenantStatus.Suspended)
        {
            throw new InvalidOperationException("Cannot activate a tenant that is not suspended." +
                                                "Actual Status: " + Status.ToString());
        }
        Status = SupersetTenantStatus.Active;
        StatusUpdatedAt = DateTimeOffset.UtcNow;
    }
}


        





