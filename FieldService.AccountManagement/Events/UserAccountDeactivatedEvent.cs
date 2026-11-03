namespace FieldService.AccountManagement.Events;

public class UserAccountDeactivatedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }

    protected UserAccountDeactivatedEvent() { }

    private UserAccountDeactivatedEvent(
        Guid userId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.UserAccountDeactivated, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
    }

    public static UserAccountDeactivatedEvent Create(
        Guid userId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new UserAccountDeactivatedEvent(
            userId,
            tenantId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}
