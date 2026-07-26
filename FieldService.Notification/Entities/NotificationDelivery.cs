namespace FieldService.Notification.Entities;

public class NotificationDelivery
{
    public Guid Id { get; private set; }
    public Guid NotificationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public DateTime ReceivedAt { get; }
    public DateTime? ReadAt { get; private set; }

    public NotificationDelivery(
        Guid notificationId,
        Guid userId,
        Guid deviceId,
        DateTime receivedAt,
        Guid? id = null)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        NotificationId = notificationId;
        DeviceId = deviceId;
        UserId = userId;
        ReceivedAt = receivedAt;
        Id = id ?? Guid.NewGuid();
 
    }

    protected NotificationDelivery() {}
    
    
    public void MarkRead(DateTime readAt)
    {
        if(readAt < ReceivedAt)
            throw new InvalidOperationException("Cannot mark as read before received.");
        
        if(ReadAt.HasValue && readAt < ReadAt.Value)
            throw new InvalidOperationException("Cannot mark as read before previous read.");
        
        ReadAt = readAt;
    }
    
}
