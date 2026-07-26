using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRMessageSender
{
    Task SendDeviceAsync<TPayload>(Guid deviceId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task SendUserAsync<TPayload>(Guid userId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task SendRoomAsync<TPayload>(Guid roomId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task SendTenantAsync<TPayload>(Guid tenantId, SignalRMessage<TPayload> message, CancellationToken ct = default);
    Task SendAllAsync<TPayload>(SignalRMessage<TPayload> message, CancellationToken ct = default);
}
