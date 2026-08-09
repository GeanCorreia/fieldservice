using FieldService.Shared.Services;

namespace FieldService.Authentication.Events;

public enum TenantMembershipStatus
{
    Active =1,
    Revoked =0,
}

public class UserTenantEvent
{
    public Guid Id { get; }
    public DateTime CreatedAt { get; }
    public Guid CreatedBy { get; }
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public string TenantName { get; }
    public TenantMembershipStatus Status { get; }
    public DateTime? EndedAt { get; }
    
    protected UserTenantEvent() { }
    
    private UserTenantEvent(
        DateTime createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        string tenantName,
        TenantMembershipStatus status,
        DateTime? endedAt = null,
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
        DateTime? endedAt = null
    )
    {
        return new UserTenantEvent(
            DateTimeService.GetNow(),
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
            DateTimeService.GetNow(),
            createdBy,
            userId,
            tenantId,
            tenantName,
            TenantMembershipStatus.Revoked
        );
    }
}