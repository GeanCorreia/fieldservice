using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Services;

internal sealed class SignalRRoomRegistry(
    ISignalRPresenceRegistry presenceRegistry,
    IHubContext<SignalRHub> hubContext) : ISignalRRoomRegistry
{
    public async Task AddUserToRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));

        var connections = await presenceRegistry.GetUserConnectionsAsync(userId, ct);
        foreach (var connectionId in connections)
            await hubContext.Groups.AddToGroupAsync(connectionId, $"room:{roomId}", ct);
    }

    public async Task RemoveUserFromRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));

        var connections = await presenceRegistry.GetUserConnectionsAsync(userId, ct);
        foreach (var connectionId in connections)
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, $"room:{roomId}", ct);
    }
}
