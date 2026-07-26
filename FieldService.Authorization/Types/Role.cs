namespace FieldService.Authorization.Types;

public sealed record Role
{
    public Role(Guid tenantId, RoleType type)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (!Enum.IsDefined(type))
            throw new ArgumentException("RoleType is invalid.", nameof(type));

        TenantId = tenantId;
        Type = type;
    }

    public Guid TenantId { get; }
    public RoleType Type { get; }
}
