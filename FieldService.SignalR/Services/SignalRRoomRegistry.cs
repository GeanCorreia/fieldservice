using FieldService.Cache.Interfaces;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace FieldService.SignalR.Services;

internal sealed class SignalRRoomRegistry(
    ISignalRPresenceRegistry presenceRegistry,
    IRedisContext redisContext,
    IHubContext<SignalRHub> hubContext) : ISignalRRoomRegistry
{
    private const string SessionRoomsPrefix = "signalr:session-rooms:";

    public async Task AddUserToRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        ValidateUserAndRoom(userId, roomId);
        ct.ThrowIfCancellationRequested();
        
        var recipients = await presenceRegistry.GetUserRecipientsAsync(userId, ct);
        if (recipients.Count == 0) return;

        var db = redisContext.Database;
        var roomGroupName = FormatRoomGroup(roomId);
        
        var redisTasks = recipients.Select(r =>
            db.SetAddAsync(SessionRoomsKey(r.SessionId), roomId.ToString()));
        
        var groupTasks = recipients.Select(r =>
            hubContext.Groups.AddToGroupAsync(r.ConnectionId, roomGroupName, ct));

        await Task.WhenAll(redisTasks.Concat(groupTasks));
    }

    public async Task RemoveUserFromRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        ValidateUserAndRoom(userId, roomId);
        ct.ThrowIfCancellationRequested();

        var recipients = await presenceRegistry.GetUserRecipientsAsync(userId, ct);
        if (recipients.Count == 0) return;

        var db = redisContext.Database;
        var roomGroupName = FormatRoomGroup(roomId);
        
        var redisTasks = recipients.Select(r =>
            db.SetRemoveAsync(SessionRoomsKey(r.SessionId), roomId.ToString()));
        
        var groupTasks = recipients.Select(r =>
            hubContext.Groups.RemoveFromGroupAsync(r.ConnectionId, roomGroupName, ct));

        await Task.WhenAll(redisTasks.Concat(groupTasks));
    }

    public async Task RestoreSessionRoomsAsync(string connectionId, Guid sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();
        
        var db = redisContext.Database;
        var roomMembers = await db.SetMembersAsync(SessionRoomsKey(sessionId));

        if (roomMembers.Length == 0) return;
        
        var restoreTasks = roomMembers
            .Select(m => m.ToString())
            .Where(roomIdStr => !string.IsNullOrWhiteSpace(roomIdStr) && Guid.TryParse(roomIdStr, out _))
            .Select(roomIdStr => hubContext.Groups.AddToGroupAsync(connectionId, $"room:{roomIdStr}", ct));

        await Task.WhenAll(restoreTasks);
    }
    
    public async Task UnregisterFromAllRoomsAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        ct.ThrowIfCancellationRequested();
        await presenceRegistry.UnregisterFromAllRoomsAsync(connectionId, ct);
    }

    private static void ValidateUserAndRoom(Guid userId, Guid roomId)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));
    }

    private static string FormatRoomGroup(Guid roomId) => $"room:{roomId}";
    private static string SessionRoomsKey(Guid sessionId) => $"{SessionRoomsPrefix}{sessionId}";
}