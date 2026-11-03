namespace FieldService.AccountManagement.Events;

public class TenantCreatedEvent : TenantEventBase
{
    public string Name { get; private set; }

    protected TenantCreatedEvent()
    {
        Name = null!;
    }

    private TenantCreatedEvent(
        Guid tenantId,
        string name,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TenantCreated, id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    public static TenantCreatedEvent Create(
        Guid tenantId,
        string name,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TenantCreatedEvent(
            tenantId,
            name,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

