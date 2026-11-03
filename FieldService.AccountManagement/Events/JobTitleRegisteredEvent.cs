namespace FieldService.AccountManagement.Events;

public class JobTitleRegisteredEvent : TenantEventBase
{
    public Guid JobTitleId { get; private set; }
    public string Title { get; private set; }

    protected JobTitleRegisteredEvent()
    {
        Title = null!;
    }

    private JobTitleRegisteredEvent(
        Guid jobTitleId,
        Guid tenantId,
        string title,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.JobTitleRegistered, id)
    {
        if (jobTitleId == default)
            throw new ArgumentException("JobTitleId is required.", nameof(jobTitleId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        JobTitleId = jobTitleId;
        Title = title;
    }

    public static JobTitleRegisteredEvent Create(
        Guid jobTitleId,
        Guid tenantId,
        string title,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new JobTitleRegisteredEvent(
            jobTitleId,
            tenantId,
            title,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

