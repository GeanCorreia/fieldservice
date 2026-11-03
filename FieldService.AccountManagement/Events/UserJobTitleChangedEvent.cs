namespace FieldService.AccountManagement.Events;

public class UserJobTitleChangedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public Guid JobTitleId { get; private set; }

    protected UserJobTitleChangedEvent() { }

    private UserJobTitleChangedEvent(
        Guid userId,
        Guid jobTitleId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.UserJobTitleChanged, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (jobTitleId == default)
            throw new ArgumentException("JobTitleId is required.", nameof(jobTitleId));

        UserId = userId;
        JobTitleId = jobTitleId;
    }

    public static UserJobTitleChangedEvent Create(
        Guid userId,
        Guid jobTitleId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new UserJobTitleChangedEvent(
            userId,
            jobTitleId,
            tenantId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}
