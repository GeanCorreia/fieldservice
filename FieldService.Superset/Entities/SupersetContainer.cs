using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using FieldService.Shared.Types;

namespace FieldService.Superset.Entities;

public enum ContainerStatus
{
    Active = 1,
    Suspended = 2,
    Deallocated = 3
}



public enum ExecutionType
{
    OnDemand = 1,
    Scheduled = 2,
    AlwaysOn = 3
}

public record SupersetContainerConfiguration
{
    public ExecutionType ExecutionType { get; init; }
    public int MaxReplicas { get; init; }
    public int MinReplicas { get; init; }
    public ExecutionWindow? ExecutionWindow { get; init; }

    public SupersetContainerConfiguration(
        ExecutionType executionType,
        int maxReplicas,
        int minReplicas,
        ExecutionWindow? executionWindow = null)
    {
        if (executionType != ExecutionType.Scheduled && executionWindow != null)
        {
            throw new ArgumentException(
                $"Execution window should be provided only for Scheduled execution type. " +
                $"ExecutionType: {executionType}");
        }

        if (executionType == ExecutionType.Scheduled && executionWindow == null)
        {
            throw new ArgumentException("Execution window must be provided for Scheduled execution type.");
        }

        if (minReplicas < 0 || maxReplicas < minReplicas)
        {
            throw new ArgumentException($"Invalid replica bounds: MinReplicas ({minReplicas}) cannot be greater than MaxReplicas ({maxReplicas}).");
        }

        ExecutionType = executionType;
        MaxReplicas = maxReplicas;
        MinReplicas = minReplicas;
        ExecutionWindow = executionWindow;
    }
}

public record ExecutionWindow(
    DailyExecutionWindow? Monday,
    DailyExecutionWindow? Tuesday,
    DailyExecutionWindow? Wednesday,
    DailyExecutionWindow? Thursday,
    DailyExecutionWindow? Friday,
    DailyExecutionWindow? Saturday,
    DailyExecutionWindow? Sunday) ;

public record DailyExecutionWindow(
    TimeOnly StartTime,
    TimeOnly EndTime);

internal class SupersetContainer
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid SecretKeyId { get; init; }
    public CloudProvider CloudProvider { get; private set; }
    
    [NotMapped]
    public string Name => ResourceName(TenantId);
    public string ResourceId {get; private set;}
    public string FqdnUrl { get; private set; }
    public ExecutionType ExecutionType { get; private set; }
    public int MaxReplicas { get; private set; }
    public int MinReplicas { get; private set; }
    [JsonInclude]
    private JsonElement? _executionWindow; 
    [NotMapped]
    public ExecutionWindow? ExecutionWindow
    {
        get
        {
            
            if (_executionWindow is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<ExecutionWindow>(_executionWindow.Value)
                   ?? throw new InvalidOperationException("Failed to deserialize execution window.");
        }
    }
    public ContainerStatus Status { get; set; }
    
    protected SupersetContainer() { }
    
    public SupersetContainer(
        Guid id,
        Guid tenantId,
        CloudProvider cloudProvider,
        Guid secretKeyId,
        string resourceId,
        string fqdnUrl,
        ExecutionType executionType,
        int maxReplicas,
        int minReplicas,
        ContainerStatus status,
        ExecutionWindow? executionWindow = null)
    {
        Id = id;
        TenantId = tenantId;
        SecretKeyId = secretKeyId;
        CloudProvider = cloudProvider;
        ResourceId = resourceId;
        FqdnUrl = fqdnUrl;
        ExecutionType = executionType;
        MaxReplicas = maxReplicas;
        MinReplicas = minReplicas;
        _executionWindow = executionWindow is null ? null : JsonSerializer.SerializeToElement(executionWindow);
        Status = status;
        
        if(ExecutionType != ExecutionType.Scheduled && executionWindow != null)
        {
            throw new ArgumentException(
                $"Execution window should be provided only for Scheduled execution type. " +
                $"Execution: {nameof(ExecutionType)}: {ExecutionType}");
        }
        
        if(ExecutionType == ExecutionType.Scheduled && executionWindow == null)
        {
            throw new ArgumentException(
                $"Execution window should be provided for Scheduled execution type.");
        }
        
    }
    
    public static string ResourceName(Guid tenantId)
    {
        return $"superset_{tenantId:N}";
    }
    
    [NotMapped]
    public SupersetContainerConfiguration Configuration => GetConfiguration();
    
    private SupersetContainerConfiguration GetConfiguration()
    {
        return new SupersetContainerConfiguration(
            ExecutionType,
            MaxReplicas,
            MinReplicas,
            ExecutionWindow);
    }
    
    public void UpdateConfiguration(SupersetContainerConfiguration newConfig)
    {
        if (newConfig.Equals(GetConfiguration()))
        {
            return;
        }
        
        ExecutionType = newConfig.ExecutionType;
        MaxReplicas = newConfig.MaxReplicas;
        MinReplicas = newConfig.MinReplicas;
        _executionWindow = newConfig.ExecutionWindow is null ? null : JsonSerializer.SerializeToElement(newConfig.ExecutionWindow);
    }
    
    public void UpdateProviderInfo(CloudProvider cloudProvider, string resourceId, string fqdnUrl)
    {
        CloudProvider = cloudProvider;
        ResourceId = resourceId;
        FqdnUrl = fqdnUrl;
    }
    
}