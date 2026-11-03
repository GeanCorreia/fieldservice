namespace FieldService.AccountManagement.Events;

public class TeamDescriptionUpdatedEvent : TenantEventBase
{
    public Guid TeamId { get; private set; }
    public string? Description { get; private set; }

    protected TeamDescriptionUpdatedEvent() { }

    private TeamDescriptionUpdatedEvent(
        Guid teamId,
        Guid tenantId,
        string? description,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TeamDescriptionUpdated, id)
    {
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        TeamId = teamId;
        Description = description;
    }

    public static TeamDescriptionUpdatedEvent Create(
        Guid teamId,
        Guid tenantId,
        string? description,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TeamDescriptionUpdatedEvent(
            teamId,
            tenantId,
            description,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

