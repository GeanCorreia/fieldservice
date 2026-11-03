namespace FieldService.AccountManagement.Events;

public class UserAccountRegisteredEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid? JobTitleId { get; private set; }

    protected UserAccountRegisteredEvent() { }

    private UserAccountRegisteredEvent(
        Guid userId,
        Guid tenantId,
        Guid teamId,
        Guid? jobTitleId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.UserAccountRegistered, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        UserId = userId;
        TeamId = teamId;
        JobTitleId = jobTitleId;
    }

    public static UserAccountRegisteredEvent Create(
        Guid userId,
        Guid tenantId,
        Guid teamId,
        Guid? jobTitleId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new UserAccountRegisteredEvent(
            userId,
            tenantId,
            teamId,
            jobTitleId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

