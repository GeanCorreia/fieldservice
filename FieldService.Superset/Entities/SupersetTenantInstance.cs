using System.Text.Json.Serialization;
using FieldService.Cloud.Entities;

namespace FieldService.Superset.Entities;

internal class SupersetTenantInstance
{
    public Guid TenantId { get; init; }
    [JsonInclude]
    public ContainerInstanceStatus Status { get; private set; }
    [JsonInclude]
    public SupersetTenant Tenant { get; private set; } = default!;

    [JsonInclude]
    private ICollection<SupersetHealthCheck> _healthCheck = new List<SupersetHealthCheck>();

    protected SupersetTenantInstance() { }

    private SupersetTenantInstance(
        Guid tenantId,
        SupersetTenant tenant,
        ContainerInstanceStatus status)
    {
        TenantId = tenantId;
        Tenant = tenant;
        Status = status;
    }

    public static SupersetTenantInstance Create(
        SupersetTenant tenant,
        ContainerInstanceStatus status)
    {
        return new SupersetTenantInstance(
            tenantId: tenant.TenantId,
            tenant: tenant,
            status: status);
    }
    
}

internal class SupersetHealthCheck
{
    public Guid Id { get; init; }
    public string AzureResourceId { get; init; } = string.Empty;
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