using FieldService.Notification.Interfaces;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;

namespace FieldService.Notification.Services;

internal sealed class NotificationPersistentSender(
    ISignalRMessageSender signalRMessageSender,
    INotificationRepository notificationRepository,
    INotificationCache notificationCache) : INotificationPersistentSender
{
    public Task<Entities.Notification> SendDeviceAsync<TPayload>(Guid deviceId, SignalRMessage<TPayload> message, CancellationToken ct = default)
    {
        if (deviceId == default)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        ArgumentNullException.ThrowIfNull(message);

        return DispatchAsync(
            message,
            () => signalRMessageSender.SendDeviceAsync(deviceId, message, ct),
            ct);
    }

    public Task<Entities.Notification> SendUserAsync<TPayload>(Guid userId, SignalRMessage<TPayload> message, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(message);

        return DispatchAsync(
            message,
            () => signalRMessageSender.SendUserAsync(userId, message, ct),
            ct);
    }

    public Task<Entities.Notification> SendRoomAsync<TPayload>(Guid roomId, SignalRMessage<TPayload> message, CancellationToken ct = default)
    {
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));
        ArgumentNullException.ThrowIfNull(message);

        return DispatchAsync(
            message,
            () => signalRMessageSender.SendRoomAsync(roomId, message, ct),
            ct);
    }

    public Task<Entities.Notification> SendTenantAsync<TPayload>(Guid tenantId, SignalRMessage<TPayload> message, CancellationToken ct = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(message);

        return DispatchAsync(
            message,
            () => signalRMessageSender.SendTenantAsync(tenantId, message, ct),
            ct);
    }

    public Task<Entities.Notification> SendAllAsync<TPayload>(SignalRMessage<TPayload> message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return DispatchAsync(
            message,
            () => signalRMessageSender.SendAllAsync(message, ct),
            ct);
    }

    private async Task<Entities.Notification> DispatchAsync<TPayload>(
        SignalRMessage<TPayload> message,
        Func<Task> send,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var notification = Entities.Notification.Create(message);
        await notificationRepository.Save(notification, ct);
        await notificationCache.SetPayloadByMessageId(notification.Id, notification.Payload, ct);

        await send();
        
        return notification;
    }
}
