using FieldService.Shared.Types;

namespace FieldService.Authorization.Events;

public enum PermissionEventType
{
    PermissionGrantedEvent = 1,
    PermissionRevokedEvent = 0
}

public class PermissionEvent
{
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public Permission Permission { get; private set; }
    public PermissionEventType Type { get; private set; }

    protected PermissionEvent()
    {
        Permission = null!;
    }

    private PermissionEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        Permission permission,
        PermissionEventType type,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        UserId = userId;
        TenantId = tenantId;
        Permission = permission;
        Type = type;
    }
    
    public static PermissionEvent CreatePermissionGrantedEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        Permission permission,
        Guid? id = null)
    {
        return new PermissionEvent(
            createdAt,
            createdBy,
            userId,
            tenantId,
            permission,
            PermissionEventType.PermissionGrantedEvent,
            id);
    }

    public static PermissionEvent CreatePermissionRevokedEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        Permission permission,
        Guid? id = null)
    {
        return new PermissionEvent(
            createdAt,
            createdBy,
            userId,
            tenantId,
            permission,
            PermissionEventType.PermissionRevokedEvent);

    }
}