using FieldService.Shared.Types;

namespace FieldService.Authorization.Events;


public class RoleEvent
{
    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public Guid CreatedBy { get; }
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public Role Role { get; }
  
    
    protected RoleEvent() { }
    public RoleEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        Role role,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        UserId = userId;
        TenantId = tenantId;
        Role = role;
    }

    public static RoleEvent CreateRoleAssignmentEvent(
        DateTimeOffset createdAt, 
        Guid createdBy, 
        Guid userId, 
        Guid tenantId,
        Role role)
    {
        return new RoleEvent(
            createdAt,
            createdBy,
            userId,
            tenantId,
            role);
    }

}

