namespace FieldService.AccountManagement.Events;

public class TeamLeaderUpdatedEvent : TenantEventBase
{
    public Guid TeamId { get; private set; }
    public Guid? LeaderId { get; private set; }

    protected TeamLeaderUpdatedEvent() { }

    private TeamLeaderUpdatedEvent(
        Guid teamId,
        Guid tenantId,
        Guid? leaderId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TeamLeaderUpdated, id)
    {
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        TeamId = teamId;
        LeaderId = leaderId;
    }

    public static TeamLeaderUpdatedEvent Create(
        Guid teamId,
        Guid tenantId,
        Guid? leaderId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TeamLeaderUpdatedEvent(
            teamId,
            tenantId,
            leaderId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

