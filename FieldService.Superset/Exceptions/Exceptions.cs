using FieldService.Superset.Entities;

namespace FieldService.Superset.Exceptions;

public class SupersetTenantNotFoundException : Exception
{
    public Guid TenantId { get; }

    public SupersetTenantNotFoundException(Guid tenantId)
        : base($"Superset tenant ID '{tenantId}' was not found.")
    {
        TenantId = tenantId;
    }
}

public class SupersetTenantFlowNotFoundException : Exception
{
    public Guid Id { get; }

    public SupersetTenantFlowNotFoundException(Guid id)
        : base($"Superset tenant flow ID '{id}' was not found.")
    {
        Id = id;
    }
}

public class SupersetContainerNotFoundException : Exception
{
    public Guid ContainerId { get; }

    public SupersetContainerNotFoundException(Guid containerId)
        : base($"Superset container ID '{containerId}' was not found.")
    {
        ContainerId = containerId;
    }
}

public class SupersetTenantBlockedException : Exception
{
    public Guid TenantId { get; }
    public SupersetTenantStatus Status { get; }

    public SupersetTenantBlockedException(
        Guid tenantId, 
        SupersetTenantStatus status, 
        string? message = null)
        : base(message ?? $"Superset tenant ID '{tenantId}' is blocked with status '{status}'.")
    {
        TenantId = tenantId;
        Status = status;
    }
}

public class SupersetTenantSuspendedException : SupersetTenantBlockedException
{
    public SupersetTenantSuspendedException(Guid tenantId)
        : base(
            tenantId, 
            SupersetTenantStatus.Suspended, 
            $"Infrastructure operation blocked: Tenant '{tenantId}' is currently suspended.")
    {
    }
}

public class SupersetTenantCancelledException : SupersetTenantBlockedException
{
    public SupersetTenantCancelledException(Guid tenantId)
        : base(
            tenantId, 
            SupersetTenantStatus.Cancelled, 
            $"Infrastructure operation blocked: Tenant '{tenantId}' has been cancelled.")
    {
    }
}
public class ResourceIdNotFoundException : Exception
{
    public string ResourceId { get; }

    public ResourceIdNotFoundException(string resourceId)
        : base($"Resource ID '{resourceId}' was not found.")
    {
        ResourceId = resourceId;
    }
}

public class SupersetTenantInstanceNotRunningException : Exception
{
    public Guid TenantId { get; }

    public SupersetTenantInstanceNotRunningException(Guid tenantId)
        : base($"Superset tenant instance for tenant ID '{tenantId}' is not running.")
    {
        TenantId = tenantId;
    }
}

