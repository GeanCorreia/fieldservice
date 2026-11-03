namespace FieldService.AccountManagement.Events;

public class TeamDeactivatedEvent : TenantEventBase
{
    public Guid TeamId { get; private set; }

    protected TeamDeactivatedEvent() { }

    private TeamDeactivatedEvent(
        Guid teamId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TeamDeactivated, id)
    {
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        TeamId = teamId;
    }

    public static TeamDeactivatedEvent Create(
        Guid teamId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TeamDeactivatedEvent(
            teamId,
            tenantId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

