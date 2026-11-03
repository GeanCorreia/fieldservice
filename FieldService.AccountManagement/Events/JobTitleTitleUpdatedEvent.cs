namespace FieldService.AccountManagement.Events;

public class JobTitleTitleUpdatedEvent : TenantEventBase
{
    public Guid JobTitleId { get; private set; }
    public string Title { get; private set; }

    protected JobTitleTitleUpdatedEvent()
    {
        Title = null!;
    }

    private JobTitleTitleUpdatedEvent(
        Guid jobTitleId,
        Guid tenantId,
        string title,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.JobTitleTitleUpdated, id)
    {
        if (jobTitleId == default)
            throw new ArgumentException("JobTitleId is required.", nameof(jobTitleId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        JobTitleId = jobTitleId;
        Title = title.Trim();
    }

    public static JobTitleTitleUpdatedEvent Create(
        Guid jobTitleId,
        Guid tenantId,
        string title,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new JobTitleTitleUpdatedEvent(
            jobTitleId,
            tenantId,
            title,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

