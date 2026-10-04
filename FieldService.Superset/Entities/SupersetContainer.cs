using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldService.Superset.Entities;

public enum SupersetContainerStatus
{
    Active = 1,
    Suspended = 2,
    Deallocated = 3
}

public enum ProviderType
{
    Azure = 1,
    Aws = 2,
    Gcp = 3
}

public enum ExecutionType
{
    OnDemand = 1,
    Scheduled = 2,
    AlwaysOn = 3
}

public record SupersetContainerConfiguration(
    ExecutionType ExecutionType,
    int MaxReplicas,
    int MinReplicas,
    ExecutionWindow? ExecutionWindow = null);

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
    public ProviderType ProviderType { get; init; }
    
    [NotMapped]
    public string Name => ResourceName(TenantId);
    public string ResourceId {get; init;}
    public string FqdnUrl { get; init; }
    public ExecutionType ExecutionType { get; init; }
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
    public SupersetContainerStatus Status { get; set; }
    
    protected SupersetContainer() { }
    
    public SupersetContainer(
        Guid id,
        Guid tenantId,
        ProviderType providerType,
        Guid secretKeyId,
        string resourceId,
        string fqdnUrl,
        ExecutionType executionType,
        int maxReplicas,
        int minReplicas,
        SupersetContainerStatus status,
        ExecutionWindow? executionWindow = null)
    {
        Id = id;
        TenantId = tenantId;
        SecretKeyId = secretKeyId;
        ProviderType = providerType;
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
            ExecutionType: ExecutionType,
            MaxReplicas: MaxReplicas,
            MinReplicas: MinReplicas,
            ExecutionWindow: ExecutionWindow);
    }
    
}