using FieldService.Shared.Message;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Services;

internal sealed class SignalRMessageSender(IHubContext<SignalRHub> hubContext) : ISignalRMessageSender
{
    private const string NotificationEvent = "notification";

    public Task SendDeviceAsync<TPayload>(Guid deviceId, Message<TPayload> message, CancellationToken ct = default)
    {
        if (deviceId == default)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"device:{deviceId}").SendAsync(NotificationEvent, message, ct);
    }

    public Task SendUserAsync<TPayload>(Guid userId, Message<TPayload> message, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"user:{userId}").SendAsync(NotificationEvent, message, ct);
    }

    public Task SendRoomAsync<TPayload>(Guid roomId, Message<TPayload> message, CancellationToken ct = default)
    {
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"room:{roomId}").SendAsync(NotificationEvent, message, ct);
    }

        public Task SendTenantAsync<TPayload>(Guid tenantId, Message<TPayload> message, CancellationToken ct = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(message);

        return hubContext.Clients.Group($"tenant:{tenantId}").SendAsync(NotificationEvent, message, ct);
    }

    public Task SendAllAsync<TPayload>(Message<TPayload> message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return hubContext.Clients.All.SendAsync(NotificationEvent, message, ct);
    }
}
