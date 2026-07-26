using FieldService.Notification.Interfaces;

namespace FieldService.Notification.Services;

internal sealed class NotificationPersistentReceiver(
    INotificationRepository notificationRepository) : INotificationPersistentReceiver
{
    public async Task AckDeliveredAsync(
        Guid notificationId,
        Guid deviceId,
        Guid userId,
        DateTime? receivedAt = null,
        CancellationToken ct = default)
    {
        var notification = await GetRequiredNotificationWithDeliveriesAsync(notificationId, ct);
        notification.MarkDelivered(userId, deviceId, receivedAt ?? DateTime.UtcNow);
        await notificationRepository.Save(notification, ct);
    }

    public async Task AckReadAsync(
        Guid notificationId,
        Guid deviceId,
        Guid userId,
        DateTime? readAt = null,
        CancellationToken ct = default)
    {
        var notification = await GetRequiredNotificationWithDeliveriesAsync(notificationId, ct);
        notification.MarkRead(userId, deviceId, readAt ?? DateTime.UtcNow);
        await notificationRepository.Save(notification, ct);
    }

    private async Task<Entities.Notification> GetRequiredNotificationWithDeliveriesAsync(Guid notificationId, CancellationToken ct)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        var notification = await notificationRepository.GetById(notificationId, ct)
                           ?? throw new InvalidOperationException("Notification was not found.");

        var deliveries = await notificationRepository.GetNotificationDeliveries(notificationId, ct);
        var aggregate = new Entities.Notification(
            notification.TargetContext,
            notification.CreatedAt,
            notification.Payload,
            deliveries,
            notification.Id);

        if (notification.DeletedAt.HasValue)
            aggregate.MarkDeleted(notification.DeletedAt.Value);

        return aggregate;
    }
}
