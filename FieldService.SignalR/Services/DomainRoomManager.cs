using FieldService.SignalR.Configuration;
using FieldService.SignalR.Dtos;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace FieldService.SignalR.Services;

internal sealed class DomainRoomManager(
    IDomainRoomService domainRoomService,
    IHubContext<SignalRHub> hubContext,
    HybridCache hybridCache,
    IOptions<SignalROptions> options) : IDomainRoomManager
{
    private readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(options.Value.TtlRoomRegistryInHours),
        LocalCacheExpiration = TimeSpan.FromMinutes(30)
    };

    private const string ConnectionRoomsPrefix = "signalr:conn-rooms:";
    private static string ConnectionRoomsKey(string connectionId) => $"{ConnectionRoomsPrefix}{connectionId}";
    private static string FormatRoomGroup(Guid roomId) => $"room:{roomId}";

    public async Task OnConnectedAsync(SignalRConnectionContext context, CancellationToken ct = default)
    {

        var userRooms = await domainRoomService.GetRoomsForUserAsync(context.UserId, ct);
        if (!userRooms.Any()) return;
        
        var roomIds = userRooms.Select(r => r.Id).ToList();
        
        var key = ConnectionRoomsKey(context.ConnectionId);
        await hybridCache.SetAsync(key, new HashSet<Guid>(roomIds), _cacheOptions, cancellationToken: ct);
        
        var groupTasks = roomIds.Select(roomId =>
            hubContext.Groups.AddToGroupAsync(context.ConnectionId, FormatRoomGroup(roomId), ct));

        await Task.WhenAll(groupTasks);
    }

    public async Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default)
    {
        var key = ConnectionRoomsKey(connectionId);
        
        var activeRooms = await hybridCache.GetOrCreateAsync(
            key,
            _ => ValueTask.FromResult(new HashSet<Guid>()),
            _cacheOptions,
            cancellationToken: ct);

        if (activeRooms.Count > 0)
        {
            var groupTasks = activeRooms.Select(roomId =>
                hubContext.Groups.RemoveFromGroupAsync(connectionId, FormatRoomGroup(roomId), ct));

            await Task.WhenAll(groupTasks);
        }
        
        await hybridCache.RemoveAsync(key, ct);
    }

    public async Task JoinRoomAsync(DomainRoomRegistryDto entry, CancellationToken ct = default)
    {
        await JoinRoomAsync([entry], ct);
    }

    public async Task JoinRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default)
    {
        var entryList = entries as List<DomainRoomRegistryDto> ?? entries.ToList();
        if (entryList.Count == 0) return;

        var tasks = entryList.Select(entry => ProcessJoinEntryAsync(entry, ct));
        await Task.WhenAll(tasks);
    }

    public async Task LeaveRoomAsync(DomainRoomRegistryDto entry, CancellationToken ct = default)
    {
        await LeaveRoomAsync([entry], ct);
    }

    public async Task LeaveRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default)
    {
        var entryList = entries as List<DomainRoomRegistryDto> ?? entries.ToList();
        if (entryList.Count == 0) return;

        var tasks = entryList.Select(entry => ProcessLeaveEntryAsync(entry, ct));
        await Task.WhenAll(tasks);
    }

    #region Helpers Privados Paraleilizados

    private async Task ProcessJoinEntryAsync(DomainRoomRegistryDto entry, CancellationToken ct)
    {
        var room = await domainRoomService.GetRoomAsync(entry.RoomId, ct);
        if (room is null || !room.HasMember(entry.User.Id)) return;
        
        await domainRoomService.AddToRoomAsync(entry, ct);
        
        await hubContext.Groups.AddToGroupAsync(
            entry.User.Id.ToString(), 
            FormatRoomGroup(entry.RoomId), 
            ct);
    }

    private async Task ProcessLeaveEntryAsync(DomainRoomRegistryDto entry, CancellationToken ct)
    {
        await domainRoomService.RemoveFromRoomAsync(entry, ct);
        
        await hubContext.Groups.RemoveFromGroupAsync(
            entry.User.Id.ToString(), 
            FormatRoomGroup(entry.RoomId), 
            ct);
    }

    #endregion
}