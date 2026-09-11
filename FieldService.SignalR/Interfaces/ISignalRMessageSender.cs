using FieldService.Shared.Message;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRMessageSender
{
    Task SendSessionAsync<TPayload>(Guid sessionId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload;

    Task SendUserAsync<TPayload>(Guid userId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload;

    Task SendRoomAsync<TPayload>(Guid roomId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload;

    Task SendTenantAsync<TPayload>(Guid tenantId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload;

    Task SendAllAsync<TPayload>(IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload;
}