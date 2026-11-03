namespace FieldService.AccountManagement.Events;

public abstract class TenantEventBase
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public Guid CreatedBy { get; protected set; }
    public Guid TenantId { get; protected set; }
    public TenantEventType EventType { get; protected set; }

    protected TenantEventBase() { }

    protected TenantEventBase(
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        TenantEventType eventType,
        Guid? id = null)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        TenantId = tenantId;
        EventType = eventType;
    }
}

