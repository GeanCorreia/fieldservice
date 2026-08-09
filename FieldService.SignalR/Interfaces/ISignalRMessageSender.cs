using FieldService.Shared.Message;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRMessageSender
{
    Task SendDeviceAsync<TPayload>(Guid deviceId, Message<TPayload> message, CancellationToken ct = default);
    Task SendUserAsync<TPayload>(Guid userId, Message<TPayload> message, CancellationToken ct = default);
    Task SendRoomAsync<TPayload>(Guid roomId, Message<TPayload> message, CancellationToken ct = default);
    Task SendTenantAsync<TPayload>(Guid tenantId, Message<TPayload> message, CancellationToken ct = default);
    Task SendAllAsync<TPayload>(Message<TPayload> message, CancellationToken ct = default);
}
