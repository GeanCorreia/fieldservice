namespace FieldService.AccountManagement.Events;

public class TenantNameUpdatedEvent : TenantEventBase
{
    public string Name { get; private set; }

    protected TenantNameUpdatedEvent()
    {
        Name = null!;
    }

    private TenantNameUpdatedEvent(
        Guid tenantId,
        string name,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TenantNameUpdated, id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    public static TenantNameUpdatedEvent Create(
        Guid tenantId,
        string name,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TenantNameUpdatedEvent(
            tenantId,
            name,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

