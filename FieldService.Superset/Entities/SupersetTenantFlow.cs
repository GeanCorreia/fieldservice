using Amazon.S3.Model;
using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;

namespace FieldService.Superset.Entities;

internal enum FlowType
{
    Creation,
    Migration
}

public enum SupersetTenantDeployStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}
internal class SupersetTenantFlow 
{
    public Guid Id { get; init; }
    public FlowType FlowType { get; init; }
    public Guid TenantId { get; init; }
    public SupersetTenantCreateParams CreateParams { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? ContainerCreatedAt { get; private set; }
    public Guid? CreatedContainerId { get; private set; }
    public Guid? SecretKeyId { get; private set; }
    public DateTimeOffset? DataSchemaCreatedAt { get; private set; }
    public DateTimeOffset? RolesCreatedAt { get; private set; }
    public Guid? ConnectionStringId { get; private set; }
    public Guid? YamlMigrationFileId { get; private set; }
    public DateTimeOffset? YamlMigrationFileCreatedAt { get; private set; }
    public DateTimeOffset? YamlMigrationFileDeletedAt { get; private set; }
    public DateTimeOffset? YamlMigrationFileUpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    
    protected SupersetTenantFlow() { }

    private SupersetTenantFlow(
        Guid id, 
        Guid tenantId, 
        SupersetTenantCreateParams createParams, 
        DateTimeOffset startedAt, 
        DateTimeOffset? containerCreatedAt = null, 
        Guid? createdContainerId = null, 
        Guid? secretKeyId = null, 
        DateTimeOffset? dataSchemaCreatedAt = null,
        DateTimeOffset? rolesCreatedAt = null,
        Guid? connectionStringId = null, 
        DateTimeOffset? completedAt = null, 
        DateTimeOffset? cancelledAt = null)
    {
        Id = id;
        TenantId = tenantId;
        CreateParams = createParams;
        StartedAt = startedAt;
        ContainerCreatedAt = containerCreatedAt;
        CreatedContainerId = createdContainerId;
        SecretKeyId = secretKeyId;
        DataSchemaCreatedAt = dataSchemaCreatedAt;
        RolesCreatedAt = rolesCreatedAt;
        ConnectionStringId = connectionStringId;
        CompletedAt = completedAt;
        CancelledAt = cancelledAt;
    }

    public static SupersetTenantFlow Create(
        SupersetTenantCreateParams createParams,
        Guid tenantId)
    {
        return new SupersetTenantFlow(
            Guid.NewGuid(), 
            tenantId, 
            createParams,
            DateTimeOffset.UtcNow);
    }
    
    public void MarkContainerCreated(Guid containerId, Guid secretKeyId, DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        if(CancelledAt.HasValue)
            throw new InvalidOperationException("Cannot mark as created after being cancelled.");
        
        if(StartedAt > at)
            throw new InvalidOperationException("Cannot mark as created before the start time.");
        
        CompletedAt = at;
        CreatedContainerId = containerId;
        SecretKeyId = secretKeyId;
        
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
    
    public void MarkAsCancelled()
    {
        if(CompletedAt.HasValue)
            throw new InvalidOperationException("Cannot cancel a creation that has already been created.");
        
        if(StartedAt > DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Cannot cancel a creation before the start time.");
        
        CancelledAt = DateTimeOffset.UtcNow;
    }
    
    public void MarkYamlMigrationFileCreated(Guid fileId, DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        if(CancelledAt.HasValue)
            throw new InvalidOperationException("Cannot mark as YAML migration file created after being cancelled.");
        
        if(StartedAt > at)
            throw new InvalidOperationException("Cannot mark as YAML migration file created before the start time.");
        
        YamlMigrationFileId = fileId;
        YamlMigrationFileCreatedAt = at;
    }
    
    
    public SupersetTenantDeployStatus Status => GetStatus();

    private SupersetTenantDeployStatus GetStatus()
    {
        if (FlowType == FlowType.Creation)
            return GetStatusCreation();
        
        if (FlowType == FlowType.Migration)
            return GetStatusMigration();
        
        throw new InvalidOperationException("Invalid flow type for status retrieval.");
    }
    private SupersetTenantDeployStatus GetStatusCreation()
    {
        if (CancelledAt.HasValue)
            return SupersetTenantDeployStatus.Cancelled;
        
        if (CompletedAt.HasValue)
            return SupersetTenantDeployStatus.Completed;
        
        if (ContainerCreatedAt.HasValue || DataSchemaCreatedAt.HasValue)
            return SupersetTenantDeployStatus.InProgress;
        
        return SupersetTenantDeployStatus.Pending;
    }
    
    private SupersetTenantDeployStatus GetStatusMigration()
    {
        if (CancelledAt.HasValue)
            return SupersetTenantDeployStatus.Cancelled;
        
        if (CompletedAt.HasValue)
            return SupersetTenantDeployStatus.Completed;
        
        if (YamlMigrationFileCreatedAt.HasValue || YamlMigrationFileUpdatedAt.HasValue || YamlMigrationFileDeletedAt.HasValue)
            return SupersetTenantDeployStatus.InProgress;
        
        return SupersetTenantDeployStatus.Pending;
    }
    
    public void MarkRolesCreated(DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        if(CancelledAt.HasValue)
            throw new InvalidOperationException("Cannot mark as roles created after being cancelled.");
        
        if(StartedAt > at)
            throw new InvalidOperationException("Cannot mark as roles created before the start time.");
        
        RolesCreatedAt = at;
    }
    
    

    
}