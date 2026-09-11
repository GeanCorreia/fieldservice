namespace FieldService.Authentication.Events;

public enum TenantMembershipStatus
{
    Active =1,
    Revoked =0,
}

public class UserTenantEvent
{
    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public Guid CreatedBy { get; }
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public string TenantName { get; }
    public TenantMembershipStatus Status { get; }
    public DateTimeOffset? EndedAt { get; }
    
    protected UserTenantEvent() { }
    
    private UserTenantEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        string tenantName,
        TenantMembershipStatus status,
        DateTimeOffset? endedAt = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        UserId = userId;
        TenantId = tenantId;
        TenantName = tenantName;
        Status = status;
        EndedAt = endedAt;
    }

    public static UserTenantEvent Active(
        Guid userId,
        Guid tenantId,
        string tenantName,
        Guid createdBy,
        DateTimeOffset? endedAt = null
    )
    {
        return new UserTenantEvent(
            DateTimeOffset.UtcNow,
            createdBy,
            userId,
            tenantId,
            tenantName,
            TenantMembershipStatus.Active,
            endedAt
        );
    }
    
    public static UserTenantEvent Revoked(
        Guid userId,
        Guid tenantId,
        string tenantName,
        Guid createdBy
    )
    {
        return new UserTenantEvent(
            DateTimeOffset.UtcNow,
            createdBy,
            userId,
            tenantId,
            tenantName,
            TenantMembershipStatus.Revoked
        );
    }
}