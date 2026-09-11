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
    public DateTimeOffset CreatedAt { get; }
    public Guid CreatedBy { get; }
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public SuspensionEventType Type { get; }
    public SuspensionSource Source { get; }
    public DateTimeOffset? StartedAt { get; }
    public DateTimeOffset? EndedAt { get; }
    
    public Guid? OriginalSuspensionEventId { get; }
    
    protected UserSuspensionEvent() { }

    private UserSuspensionEvent(
        DateTimeOffset createdAt,
        Guid createdBy,
        Guid userId,
        Guid tenantId,
        SuspensionEventType type,
        SuspensionSource source,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? endedAt = null,
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
        DateTimeOffset createdAt,
        Guid userId,
        Guid tenantId,
        SuspensionSource source,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt
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
        DateTimeOffset createdAt,
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
        DateTimeOffset createdAt,
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
