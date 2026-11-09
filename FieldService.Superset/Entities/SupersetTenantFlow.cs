using System.ComponentModel.DataAnnotations.Schema;
using FieldService.Cloud.Entities;
using FieldService.Shared.Enums;


namespace FieldService.Superset.Entities;

public class SupersetTenantFlow 
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public ContainerConfiguration Configuration { get; init; }
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
    
    
    protected SupersetTenantFlow() { }

    private SupersetTenantFlow(
        Guid id, 
        Guid tenantId, 
        ContainerConfiguration configuration, 
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
    
    

    public static SupersetTenantFlow Create(
        ContainerConfiguration configuration,
        Guid tenantId,
        Guid? dedicatedHostConnectionStringId = null)
    {
        return new SupersetTenantFlow(
            Guid.NewGuid(), 
            tenantId, 
            configuration,
            DateTimeOffset.UtcNow,
            customHostConnectionStringId: dedicatedHostConnectionStringId);
    }
    
    public static string SupersetTenantFlowContext(Guid flowId) =>
        $"superset-tenant-container-deployment-flow-{flowId}";

    public string SupersetTenantFlowContext() =>
        SupersetTenantFlowContext(Id);

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
    
    public FlowStatus Status => GetStatus();

    private FlowStatus GetStatus()
    {
     
        if (CancelledAt.HasValue)
            return FlowStatus.Cancelled;
        
        if (PersistedAt.HasValue)
            return FlowStatus.Completed;
        
        if (ContainerCreatedAt.HasValue || DataSchemaCreatedAt.HasValue)
            return FlowStatus.InProgress;
        
        return FlowStatus.Pending;
    }
    
    private FlowStatus GetStatusMigration()
    {
        if (CancelledAt.HasValue)
            return FlowStatus.Cancelled;
        
        if (PersistedAt.HasValue)
            return FlowStatus.Completed;
        
        if(DataSchemaCreatedAt.HasValue || ContainerCreatedAt.HasValue)
            return FlowStatus.InProgress;
        
        return FlowStatus.Pending;
    }
    
}