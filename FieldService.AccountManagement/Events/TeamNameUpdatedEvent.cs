namespace FieldService.AccountManagement.Events;

public class TeamNameUpdatedEvent : TenantEventBase
{
    public Guid TeamId { get; private set; }
    public string Name { get; private set; }

    protected TeamNameUpdatedEvent()
    {
        Name = null!;
    }

    private TeamNameUpdatedEvent(
        Guid teamId,
        Guid tenantId,
        string name,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TeamNameUpdated, id)
    {
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TeamId = teamId;
        Name = name;
    }

    public static TeamNameUpdatedEvent Create(
        Guid teamId,
        Guid tenantId,
        string name,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TeamNameUpdatedEvent(
            teamId,
            tenantId,
            name,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}


