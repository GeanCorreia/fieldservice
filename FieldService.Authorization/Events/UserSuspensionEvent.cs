namespace FieldService.Authorization.Events;

public enum SuspensionEventType
{
    Created = 1,
    Cancelled = 0
}

public enum SuspensionSource
{
    HumanResources = 1,
    Security = 2,
    Compliance = 3,
    Operations = 4,
    Administration = 5,
    System = 6
}

public class UserSuspensionEvent
{
    public Guid Id { get; }
    public DateTime CreatedAt { get; }
    public Guid CreatedBy { get; }
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public SuspensionEventType Type { get; }
    public SuspensionSource Source { get; }
    public DateTime? StartedAt { get; }
    public DateTime? EndedAt { get; }
    
    public Guid? OriginalSuspensionEventId { get; }
    
    protected UserSuspensionEvent() { }

    private UserSuspensionEvent(
        DateTime createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        SuspensionEventType type,
        SuspensionSource source,
        DateTime? startedAt = null,
        DateTime? endedAt = null,
        Guid? id = null,
        Guid? originalSuspensionEventId = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        UserId = userId;
        TenantId = tenantId;
        Type = type;
        Source = source;
        StartedAt = startedAt;
        EndedAt = endedAt;
        OriginalSuspensionEventId = originalSuspensionEventId;
    }

    public static UserSuspensionEvent CreateTemporaryUserSuspensionEvent(
        Guid createdBy,
        DateTime createdAt,
        Guid userId,
        Guid tenantId,
        SuspensionSource source,
        DateTime startedAt,
        DateTime endedAt
    )
    {
        return new UserSuspensionEvent(
            createdAt,
            createdBy,
            userId,
            tenantId,
            SuspensionEventType.Created,
            source,
            startedAt,
            endedAt
        );
    }

    public static UserSuspensionEvent CreateCancelledUserSuspensionEvent(
        Guid originalSuspensionEventId,
        Guid createdBy,
        DateTime createdAt,
        Guid userId,
        Guid tenantId,
        SuspensionSource source)
    {
        return new UserSuspensionEvent(
            createdAt,
            createdBy,
            userId,
            tenantId,
            SuspensionEventType.Cancelled,
            source,
            originalSuspensionEventId: originalSuspensionEventId
        );
    }
    
    public static UserSuspensionEvent CreateIndefiniteUserSuspensionEvent(
        Guid createdBy,
        DateTime createdAt,
        Guid userId,
        Guid tenantId,
        SuspensionSource source)
    {
        return new UserSuspensionEvent(
            createdAt,
            createdBy,
            userId,
            tenantId,
            SuspensionEventType.Created,
            source
        );
    }
}
