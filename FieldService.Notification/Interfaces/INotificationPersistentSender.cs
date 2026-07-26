using FieldService.SignalR.Types;

namespace FieldService.Notification.Interfaces;

public interface INotificationPersistentSender
{
    Task<Entities.Notification> SendDeviceAsync<TPayload>(Guid deviceId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task<Entities.Notification> SendUserAsync<TPayload>(Guid userId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task<Entities.Notification> SendRoomAsync<TPayload>(Guid roomId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task<Entities.Notification> SendTenantAsync<TPayload>(Guid tenantId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task<Entities.Notification> SendAllAsync<TPayload>(SignalRMessage<TPayload> message, CancellationToken ct = default);
}
