namespace FieldService.AccountManagement.Events;

public class TeamRegisteredEvent : TenantEventBase
{
    public Guid TeamId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid? LeaderId { get; private set; }

    protected TeamRegisteredEvent()
    {
        Name = null!;
    }

    private TeamRegisteredEvent(
        Guid teamId,
        Guid tenantId,
        string name,
        string? description,
        Guid? leaderId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.TeamRegistered, id)
    {
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TeamId = teamId;
        Name = name.Trim();
        Description = description;
        LeaderId = leaderId;
    }

    public static TeamRegisteredEvent Create(
        Guid teamId,
        Guid tenantId,
        string name,
        string? description,
        Guid? leaderId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new TeamRegisteredEvent(
            teamId,
            tenantId,
            name,
            description,
            leaderId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

