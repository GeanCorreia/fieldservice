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

public class ResourceIdNotFoundException : Exception
{
    public Guid TenantId { get; }
    public string ResourceId { get; }

    public ResourceIdNotFoundException(Guid tenantId, string resourceId)
        : base($"Resource ID '{resourceId}' for Superset tenant ID '{tenantId}' was not found.")
    {
        TenantId = tenantId;
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

