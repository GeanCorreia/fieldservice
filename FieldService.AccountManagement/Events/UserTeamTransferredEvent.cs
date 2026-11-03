namespace FieldService.AccountManagement.Events;

public class UserTeamTransferredEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public Guid TeamId { get; private set; }

    protected UserTeamTransferredEvent() { }

    private UserTeamTransferredEvent(
        Guid userId,
        Guid teamId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.UserTeamTransferred, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

       
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));
        

        UserId = userId;
        TeamId = teamId;
    }

    public static UserTeamTransferredEvent Create(
        Guid userId,
        Guid teamId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new UserTeamTransferredEvent(
            userId,
            teamId,
            tenantId,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

