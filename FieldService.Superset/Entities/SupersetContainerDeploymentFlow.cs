using System.ComponentModel.DataAnnotations.Schema;


namespace FieldService.Superset.Entities;

public enum SupersetTenantDeployStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}
internal class SupersetContainerDeploymentFlow 
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public SupersetContainerConfiguration Configuration { get; init; }
    public Guid? CustomHostConnectionStringId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? ContainerCreatedAt { get; private set; }
    public Guid? ContainerId { get; private set; }
    public DateTimeOffset? SecretKeyCreatedAt { get; private set; }
    public Guid? SecretKeyId { get; private set; }
    public DateTimeOffset? DataSchemaCreatedAt { get; private set; }
    public Guid? ConnectionStringId { get; private set; }
    public DateTimeOffset? PersistedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    
    
    protected SupersetContainerDeploymentFlow() { }

    private SupersetContainerDeploymentFlow(
        Guid id, 
        Guid tenantId, 
        SupersetContainerConfiguration configuration, 
        DateTimeOffset startedAt, 
        Guid? customHostConnectionStringId = null,
        DateTimeOffset? containerCreatedAt = null, 
        Guid? containerId = null, 
        Guid? secretKeyId = null, 
        DateTimeOffset? dataSchemaCreatedAt = null,
        Guid? connectionStringId = null, 
        DateTimeOffset? persistedAt = null, 
        DateTimeOffset? cancelledAt = null)
    {
        Id = id;
        TenantId = tenantId;
        Configuration = configuration;
        StartedAt = startedAt;
        CustomHostConnectionStringId = customHostConnectionStringId;
        ContainerCreatedAt = containerCreatedAt;
        ContainerId = containerId;
        SecretKeyId = secretKeyId;
        DataSchemaCreatedAt = dataSchemaCreatedAt;
        ConnectionStringId = connectionStringId;
        PersistedAt = persistedAt;
        CancelledAt = cancelledAt;
    }
    
    

    public static SupersetContainerDeploymentFlow Create(
        SupersetContainerConfiguration configuration,
        Guid tenantId,
        Guid? dedicatedHostConnectionStringId = null)
    {
        return new SupersetContainerDeploymentFlow(
            Guid.NewGuid(), 
            tenantId, 
            configuration,
            DateTimeOffset.UtcNow,
            customHostConnectionStringId: dedicatedHostConnectionStringId);
    }
    
    public static string SupersetContainerDeploymentFlowContext(Guid flowId) =>
        $"superset-tenant-container-deployment-flow-{flowId}";

    public string SupersetContainerDeploymentFlowContext() =>
        SupersetContainerDeploymentFlowContext(Id);

    public void MarkContainerCreated(Guid containerId, DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        if(CancelledAt.HasValue)
            throw new InvalidOperationException("Cannot mark as created after being cancelled.");
        
        if(SecretKeyId == null)
            throw new InvalidOperationException("Cannot mark as created without a secret key.");
                
        if(StartedAt > at)
            throw new InvalidOperationException("Cannot mark as created before the start time.");
        
        PersistedAt = at;
        ContainerId = containerId;
        ContainerCreatedAt = at;
    }
    
    public void MarkDataSchemaCreated(Guid connectionStringId, DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        if(CancelledAt.HasValue)
            throw new InvalidOperationException("Cannot mark as data schema created after being cancelled.");
        if(ConnectionStringId.HasValue)
            throw new InvalidOperationException("Connection string has already been set.");
        if(StartedAt > at)
            throw new InvalidOperationException("Cannot mark as data schema created before the start time.");
        
        DataSchemaCreatedAt = at;
        ConnectionStringId = connectionStringId;
    }

    public void MarkSecretKeyCreated(Guid secretKeyId, DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        if (CancelledAt.HasValue)
            throw new InvalidOperationException("Cannot mark secret key after being cancelled.");

        if (secretKeyId == default)
            throw new ArgumentException("Secret key id is required.", nameof(secretKeyId));

        if (SecretKeyId.HasValue && SecretKeyId.Value != secretKeyId)
            throw new InvalidOperationException("Secret key has already been set with a different value.");

        SecretKeyId = secretKeyId;
        SecretKeyCreatedAt = at;
    }
    
    public void MarkAsCancelled()
    {
        if(PersistedAt.HasValue)
            throw new InvalidOperationException("Cannot cancel a creation that has already been created.");
        
        if(StartedAt > DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Cannot cancel a creation before the start time.");
        
        CancelledAt = DateTimeOffset.UtcNow;
    }
    
    public SupersetTenantDeployStatus Status => GetStatus();

    private SupersetTenantDeployStatus GetStatus()
    {
     
        if (CancelledAt.HasValue)
            return SupersetTenantDeployStatus.Cancelled;
        
        if (PersistedAt.HasValue)
            return SupersetTenantDeployStatus.Completed;
        
        if (ContainerCreatedAt.HasValue || DataSchemaCreatedAt.HasValue)
            return SupersetTenantDeployStatus.InProgress;
        
        return SupersetTenantDeployStatus.Pending;
    }
    
    private SupersetTenantDeployStatus GetStatusMigration()
    {
        if (CancelledAt.HasValue)
            return SupersetTenantDeployStatus.Cancelled;
        
        if (PersistedAt.HasValue)
            return SupersetTenantDeployStatus.Completed;
        
        if(DataSchemaCreatedAt.HasValue || ContainerCreatedAt.HasValue)
            return SupersetTenantDeployStatus.InProgress;
        
        return SupersetTenantDeployStatus.Pending;
    }
    
}