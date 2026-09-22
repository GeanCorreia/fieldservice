using FieldService.Shared.Message;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Services;

internal sealed class SignalRMessageSender(
    IPresenceRegistry presenceRegistry,
    IHubContext<SignalRHub> hubContext) : ISignalRMessageSender
{
    private const string NotificationEvent = "notification";

    public async Task SendSessionAsync<TPayload>(Guid sessionId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        ArgumentNullException.ThrowIfNull(message);

        var sessionConnection = await presenceRegistry.GetConnectionContextBySessionIdAsync(sessionId, ct);
        if (sessionConnection is null)
            return;

        await hubContext.Clients.Client(sessionConnection.ConnectionId).SendAsync(NotificationEvent, message, ct);
    }

    public Task SendUserAsync<TPayload>(Guid userId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"user:{userId}").SendAsync(NotificationEvent, message, ct);
    }

    public Task SendRoomAsync<TPayload>(Guid roomId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload
    {
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"room:{roomId}").SendAsync(NotificationEvent, message, ct);
    }

    public Task SendTenantAsync<TPayload>(Guid tenantId, IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"tenant:{tenantId}").SendAsync(NotificationEvent, message, ct);
    }

    public Task SendAllAsync<TPayload>(IMessage<TPayload> message, CancellationToken ct = default)
        where TPayload : class, IMessagePayload
    {
        ArgumentNullException.ThrowIfNull(message);
        return hubContext.Clients.All.SendAsync(NotificationEvent, message, ct);
    }
}