using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FieldService.Superset.Dtos;
using FieldService.Superset.Enums;
using System.Text.RegularExpressions;
using FieldService.Shared.Services;

namespace FieldService.Superset.Entities;

internal enum InstanceTier
{
    OnDemand = 1,
    Dedicated = 2,
    Scheduled = 3
    
}
internal enum SupersetInstanceStatus
{

    Provisioning = 1,
    Running = 2,
    Failed = 3
}

internal record ScheduledExecutionWindow(
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IncludeSaturdays = false,
    bool IncludeSundays = false);

internal class SupersetTenantInstance
{
    public Guid TenantId { get; init; } 
    public SupersetInstanceStatus Status { get; private set; } 
    public SupersetTenantConfig TenantConfig { get; private set; }
    
    private ICollection<SupersetHealthCheck> _healthCheck = new List<SupersetHealthCheck>();
    
    protected SupersetTenantInstance() { }
    
    private SupersetTenantInstance(
        Guid tenantId, 
        SupersetTenantConfig tenantConfig,
        SupersetInstanceStatus status)
    {
        TenantId = tenantId;
        TenantConfig = tenantConfig;
        Status = status;
    }

    public static SupersetTenantInstance Create(
        string fqdnUrl,
        SupersetTenantConfig tenantConfig)
    {
        return new SupersetTenantInstance(
            tenantId: tenantConfig.TenantId,
            tenantConfig: tenantConfig,
            status: SupersetInstanceStatus.Provisioning);
    }
    
}

internal class SupersetTenantConfig
{
    public static string ContainerPrefix = "superset_tenant";
    public static string DatabasePrefix = "db_tenant";
    public static string DataSchemaPrefix = "superset_data";
    public static string MetadataSchemaPrefix = "superset";
    public static string MockedDataSchemaPrefix = "superset_mocked_data";
    public static string SupersetSecretKeyPrefix = "superset_secret_key";
    public static string ConnectionStringPrefix = "superset_connection_string";
    public Guid Id { get; init; }
    public string FqdnUrl { get; private set; } 
    public Guid SupersetSecretKeyId { get; set; }
    public Guid ConnectionStringId { get; set; }
    public InstanceTier InstanceTier { get; init; }
    public Guid TenantId { get; init; }
    public string ResourceId { get; init; }
    private JsonElement _executionWindow = new();

    [NotMapped]
    public ScheduledExecutionWindow? ExecutionWindow
    {
        get
        {
            if (InstanceTier is InstanceTier.OnDemand or InstanceTier.Dedicated)
            {
                return null;
            }

            if (_executionWindow.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<ScheduledExecutionWindow>(_executionWindow)
                   ?? throw new InvalidOperationException("Failed to deserialize execution window.");
        }
    }

    [NotMapped] public string ContainerAppName => $"{ContainerPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string DatabaseAppName => $"{DatabasePrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string DataSchemaName => $"{DataSchemaPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string MetadataSchemaName => $"{MetadataSchemaPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string MockedDataSchemaName => $"{MockedDataSchemaPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string SupersetSecretKeyName => $"{SupersetSecretKeyPrefix}_{TenantId.ToString().ToLower()}";
    [NotMapped] public string SupersetConnectionStringName => $"{ConnectionStringPrefix}_{TenantId.ToString().ToLower()}";
    
    public static string Database(Guid tenantId) => $"{DatabasePrefix}_{tenantId.ToString().ToLower()}";
    public static string Container(Guid tenantId) => $"{ContainerPrefix}_{tenantId.ToString().ToLower()}";
    public static string SecretKeyName(Guid tenantId) => $"{SupersetSecretKeyPrefix}_{tenantId.ToString().ToLower()}";
    public static string ConnectionStringName(Guid tenantId) => $"{ConnectionStringPrefix}_{tenantId.ToString().ToLower()}";

    protected SupersetTenantConfig()
    {
    }

    private SupersetTenantConfig(
        Guid id, 
        Guid tenantId, 
        InstanceTier instanceTier, 
        Guid supersetSecretKeyId, 
        Guid connectionStringId,
        string resourceId,
        string fqdnUrl,
        ScheduledExecutionWindow? executionWindow = null
        )
    {
        Id = id;
        FqdnUrl = fqdnUrl ?? string.Empty;
        SupersetSecretKeyId = supersetSecretKeyId;
        ConnectionStringId = connectionStringId;
        InstanceTier = instanceTier;
        TenantId = tenantId;
        ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));

        if (InstanceTier == InstanceTier.Scheduled && executionWindow == null)
        {
            throw new ArgumentException("Execution window should be set for Sheduled instance tier");
        }
        if (executionWindow != null)
        {
            _executionWindow = JsonSerializer.SerializeToElement(executionWindow);
        }
        
    }

    public static SupersetTenantConfig Create(
        SupersetTenantConfigParams configParams)
    {
        return new SupersetTenantConfig(
            Guid.NewGuid(),
            configParams.TenantId,
            configParams.InstanceTier,
            configParams.SupersetSecretKeyId,
            configParams.ConnectionStringId,
            configParams.ResourceId,
            configParams.FqdnUrl,
            configParams.ScheduledExecutionWindow
        );
    }
}

internal class SupersetHealthCheck
{
        public Guid Id { get; init; }
        public string AzureResourceId { get; init; }
        public Guid TenantId { get; init; }
        public DateTimeOffset CheckedAt { get; init; }
        public bool IsHealthy { get; init; }
        public int ResponseTimeMs { get; init; }
        public int HttpStatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        
        protected SupersetHealthCheck() { }
        
        private SupersetHealthCheck(
            string azureResourceId,
            Guid tenantId,
            bool isHealthy,
            int responseTimeMs,
            int httpStatusCode,
            string? errorMessage = null,
            Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            AzureResourceId = azureResourceId;
            TenantId = tenantId;
            CheckedAt = DateTimeOffset.UtcNow;
            IsHealthy = isHealthy;
            ResponseTimeMs = responseTimeMs;
            HttpStatusCode = httpStatusCode;
            ErrorMessage = errorMessage;
        }
        
        public static SupersetHealthCheck Create(
            string azureResourceId,
            Guid tenantId,
            bool isHealthy,
            int responseTimeMs,
            int httpStatusCode,
            string? errorMessage = null)
        {
            return new SupersetHealthCheck(
                azureResourceId: azureResourceId,
                tenantId: tenantId,
                isHealthy: isHealthy,
                responseTimeMs: responseTimeMs,
                httpStatusCode: httpStatusCode,
                errorMessage: errorMessage);
        }
    }
        
