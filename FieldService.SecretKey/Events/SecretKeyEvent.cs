using FieldService.SecretKey.Entities;

namespace FieldService.SecretKey.Events;

public enum SecretKeyEventType
{
    Created = 1,
    Updated = 2,
    UpdatedName = 3,
    UpdatedType = 4,
    Deleted = 5
}

internal class SecretKeyEvent
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid SecretKeyId { get; init; }
    public Guid UserId { get; init; }
    public SecretKeyEventType EventType { get; init; }
    public string? Name { get; init; }
    public string? TypeName { get; init; }
    
    protected SecretKeyEvent() { }
    private SecretKeyEvent(
        Guid id, 
        DateTimeOffset occurredAt,
        Guid secretKeyId,
        Guid userId, 
        SecretKeyEventType eventType,
        string? name = null,
        string? typeName = null)
    {
        if (eventType != SecretKeyEventType.UpdatedName  &&
            eventType != SecretKeyEventType.Created && 
            name != null)
        {
            throw new InvalidOperationException(
                $"Name must only be provided for event type {SecretKeyEventType.UpdatedName}.");
        }
        
        if(eventType != SecretKeyEventType.UpdatedType && typeName != null)
        {
            throw new InvalidOperationException(
                $"TypeName must only be provided for event type {SecretKeyEventType.UpdatedType}.");
        }

        if (eventType == SecretKeyEventType.Updated && (typeName != null || name != null))
        {
            throw new InvalidOperationException(
                $"Name and TypeName must not be provided for event type {eventType}.");
        }
        
        
        
        Id = id;
        OccurredAt = occurredAt;
        SecretKeyId = secretKeyId;
        UserId = userId;
        EventType = eventType;
        Name = name;
        TypeName = typeName;
        
    }
    
    public static SecretKeyEvent Create(
        Guid userId, 
        Guid secretKeyId,
        SecretKeyEventType eventType,
        string? name = null,
        string? typeName = null)
    {
        return new SecretKeyEvent(
            id: Guid.NewGuid(),
            occurredAt: DateTimeOffset.UtcNow,
            secretKeyId: secretKeyId,
            userId: userId,
            eventType: eventType,
            name: name,
            typeName: typeName
        );
    }
    
}