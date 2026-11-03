namespace FieldService.Authentication.Events;

public enum TenantMembershipStatus
{
    Active = 1,
    Revoked = 0,
}

public class UserTenantEvent
{
    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public Guid CreatedBy { get; }
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public TenantMembershipStatus Status { get; }
    public DateTimeOffset? EndedAt { get; }
    
    protected UserTenantEvent() { }
    
    private UserTenantEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        TenantMembershipStatus status,
        DateTimeOffset? endedAt = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        UserId = userId;
        TenantId = tenantId;
        Status = status;
        EndedAt = endedAt;
    }

    public static UserTenantEvent Active(
        Guid userId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? endedAt = null,
        DateTimeOffset? createdAt = null
    )
    {
        return new UserTenantEvent(
            createdAt ?? DateTimeOffset.UtcNow,
            createdBy,
            userId,
            tenantId,
            TenantMembershipStatus.Active,
            endedAt
        );
    }
    
    public static UserTenantEvent Revoked(
        Guid userId,
        Guid tenantId,
        Guid createdBy,
        DateTimeOffset? createdAt = null
    )
    {
        return new UserTenantEvent(
            createdAt ?? DateTimeOffset.UtcNow,
            createdBy,
            userId,
            tenantId,
            TenantMembershipStatus.Revoked
        );
    }
}