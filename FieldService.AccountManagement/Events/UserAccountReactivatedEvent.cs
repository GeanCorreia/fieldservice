namespace FieldService.AccountManagement.Events;

public class UserAccountReactivatedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }

    protected UserAccountReactivatedEvent() { }

    private UserAccountReactivatedEvent(
        Guid userId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.UserAccountReactivated, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
    }

    public static UserAccountReactivatedEvent Create(
        Guid userId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new UserAccountReactivatedEvent(
            userId,
            tenantId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}
