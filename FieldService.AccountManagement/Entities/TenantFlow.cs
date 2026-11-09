using FieldService.Superset.Entities;

namespace FieldService.AccountManagement.Entities;

public enum TenantFlowStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public enum TenantFlowType
{
    TenantDataInfraDeployment,
    SupersetDeployment,
    StorageDeployment,
    FormDeployment,
    
}


internal class TenantFlow
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public Guid SupersetContainerDeploymentFlowId { get; init; }
    public SupersetTenantFlow SupersetTenantFlow { get; init; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    protected TenantFlow() { }

    internal TenantFlow(
        Guid id,
        Guid tenantId,
        TenantFlowType flowType,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt = null,
        DateTimeOffset? cancelledAt = null)
    {
        Id = id;
        TenantId = tenantId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        CancelledAt = cancelledAt;

    }
}