using FieldService.Notification.Interfaces;
using FieldService.SignalR.Interfaces;

namespace FieldService.Bootstrap.Services;

internal sealed class NotificationSignalRAckProcessor(
    INotificationPersistentReceiver persistentReceiver) : ISignalRAckProcessor
{
    public Task AckDeliveredAsync(Guid notificationId, Guid deviceId, Guid userId, CancellationToken ct = default)
    {
        return persistentReceiver.AckDeliveredAsync(notificationId, deviceId, userId, DateTime.UtcNow, ct);
    }

    public Task AckReadAsync(Guid notificationId, Guid deviceId, Guid userId, CancellationToken ct = default)
    {
        return persistentReceiver.AckReadAsync(notificationId, deviceId, userId, DateTime.UtcNow, ct);
    }
}
