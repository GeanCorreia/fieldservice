namespace FieldService.AccountManagement.Events;

public class JobTitleDescriptionUpdatedEvent : TenantEventBase
{
    public Guid JobTitleId { get; private set; }
    public string? Description { get; private set; }

    protected JobTitleDescriptionUpdatedEvent() { }

    private JobTitleDescriptionUpdatedEvent(
        Guid jobTitleId,
        Guid tenantId,
        string? description,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.JobTitleDescriptionUpdated, id)
    {
        if (jobTitleId == default)
            throw new ArgumentException("JobTitleId is required.", nameof(jobTitleId));

        JobTitleId = jobTitleId;
        Description = description;
    }

    public static JobTitleDescriptionUpdatedEvent Create(
        Guid jobTitleId,
        Guid tenantId,
        string? description,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new JobTitleDescriptionUpdatedEvent(
            jobTitleId,
            tenantId,
            description,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

