namespace FieldService.Notification.Interfaces;

public interface INotificationPersistentReceiver
{
    Task AckDeliveredAsync(Guid notificationId, Guid deviceId, Guid userId, DateTime? receivedAt = null,
        CancellationToken ct = default);

    Task AckReadAsync(Guid notificationId, Guid deviceId, Guid userId, DateTime? readAt = null,
        CancellationToken ct = default);
}
