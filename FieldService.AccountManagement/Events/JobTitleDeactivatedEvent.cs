namespace FieldService.AccountManagement.Events;

public class JobTitleDeactivatedEvent : TenantEventBase
{
    public Guid JobTitleId { get; private set; }

    protected JobTitleDeactivatedEvent() { }

    private JobTitleDeactivatedEvent(
        Guid jobTitleId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.JobTitleDeactivated, id)
    {
        if (jobTitleId == default)
            throw new ArgumentException("JobTitleId is required.", nameof(jobTitleId));

        JobTitleId = jobTitleId;
    }

    public static JobTitleDeactivatedEvent Create(
        Guid jobTitleId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new JobTitleDeactivatedEvent(
            jobTitleId,
            tenantId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}
