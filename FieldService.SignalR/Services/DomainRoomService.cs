using FieldService.SignalR.Configuration;
using FieldService.SignalR.Dtos;
using FieldService.SignalR.Entities;
using FieldService.SignalR.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace FieldService.SignalR.Services;

internal sealed class DomainRoomService(
    IDomainRoomRepository domainRoomRepository,
    IOptions<SignalROptions> options,
    HybridCache hybridCache) : IDomainRoomService
{
    private readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(options.Value.TtlRoomRegistryInHours),
        LocalCacheExpiration = TimeSpan.FromMinutes(30)
    };

    private const string UserRoomsPrefix = "signalr:user-rooms:";
    private const string RoomPrefix = "signalr:room:";

    private static string UserRoomsKey(Guid userId) => $"{UserRoomsPrefix}{userId}";
    private static string RoomKey(Guid roomId) => $"{RoomPrefix}{roomId}";

    public async Task<DomainRoom?> GetRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        var key = RoomKey(roomId);

        return await hybridCache.GetOrCreateAsync(
            key,
            async token => await domainRoomRepository.GetRoomAsync(roomId, token),
            _cacheOptions,
            cancellationToken: ct);
    }

    public async Task<IEnumerable<DomainRoom>> GetRoomsAsync(IEnumerable<Guid> roomIds, CancellationToken ct = default)
    {
        var idList = roomIds as List<Guid> ?? roomIds.ToList();
        if (idList.Count == 0) return Enumerable.Empty<DomainRoom>();

       
        var cacheTasks = idList.Select(async id => 
        {
            var room = await hybridCache.GetOrCreateAsync<DomainRoom?>(
                RoomKey(id), 
                _ => ValueTask.FromResult<DomainRoom?>(null), 
                _cacheOptions, 
                cancellationToken: ct);

            return (Id: id, Room: room);
        });

        var cachedResults = await Task.WhenAll(cacheTasks);

        var foundRooms = cachedResults.Where(x => x.Room is not null).Select(x => x.Room!).ToList();
        var missingIds = cachedResults.Where(x => x.Room is null).Select(x => x.Id).ToList();
        
        if (missingIds.Count > 0)
        {
            var dbRooms = (await domainRoomRepository.GetRoomsAsync(missingIds, ct)).ToList();

            // 3. Atualiza o cache de todos os itens encontrados no banco em paralelo
            var cachePopulateTasks = dbRooms.Select(room =>
                hybridCache.SetAsync(RoomKey(room.Id), room, _cacheOptions, cancellationToken: ct).AsTask());

            await Task.WhenAll(cachePopulateTasks);

            foundRooms.AddRange(dbRooms);
        }

        return foundRooms;
    }

    public async Task<IEnumerable<DomainRoom>> GetRoomsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // 1. Busca no cache apenas os IDs das salas que o usuário pertence
        var userRoomIds = await GetUserRoomIdsFromCacheAsync(userId, ct);
        if (userRoomIds.Count == 0) return Enumerable.Empty<DomainRoom>();

        // 2. Busca as instâncias de DomainRoom (do Cache ou Banco) para esses IDs
        return await GetRoomsAsync(userRoomIds, ct);
    }

    public async Task SaveRoomAsync(DomainRoom room, CancellationToken ct = default)
    {
        // Persiste no repositório
        await domainRoomRepository.SaveRoomAsync(room, ct);

        // Atualiza/Invalida o cache da sala
        var key = RoomKey(room.Id);
        await hybridCache.SetAsync(key, room, _cacheOptions, cancellationToken: ct);
    }

    public async Task AddToRoomAsync(DomainRoomRegistryDto entry, CancellationToken ct = default)
    {
        await AddToRoomAsync([entry], ct);
    }

    public async Task AddToRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default)
    {
        var entryList = entries as List<DomainRoomRegistryDto> ?? entries.ToList();
        if (entryList.Count == 0) return;
        
        var tasks = entryList
            .GroupBy(e => e.User.Id)
            .Select(userGroup => AddUserRoomsBatchAsync(userGroup.Key, userGroup.Select(e => e.RoomId), ct));

        await Task.WhenAll(tasks);
    }

    public async Task RemoveFromRoomAsync(DomainRoomRegistryDto entry, CancellationToken ct = default)
    {
        await RemoveUserFromRoomAsync([entry], ct);
    }

    public async Task RemoveUserFromRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default)
    {
        var entryList = entries as List<DomainRoomRegistryDto> ?? entries.ToList();
        if (entryList.Count == 0) return;
        
        var tasks = entryList
            .GroupBy(e => e.User.Id)
            .Select(userGroup => RemoveUserRoomsBatchAsync(userGroup.Key, userGroup.Select(e => e.RoomId), ct));

        await Task.WhenAll(tasks);
    }

    public async Task UnregisterFromAllRoomsAsync(Guid userId, CancellationToken ct = default)
    {
        var key = UserRoomsKey(userId);
        await hybridCache.RemoveAsync(key, ct);
    }

    #region Private Batch Helpers

    private async Task<HashSet<Guid>> GetUserRoomIdsFromCacheAsync(Guid userId, CancellationToken ct)
    {
        var key = UserRoomsKey(userId);
        return await hybridCache.GetOrCreateAsync(
            key,
            _ => ValueTask.FromResult(new HashSet<Guid>()),
            _cacheOptions,
            cancellationToken: ct);
    }

    private async Task AddUserRoomsBatchAsync(Guid userId, IEnumerable<Guid> roomIds, CancellationToken ct)
    {
        var key = UserRoomsKey(userId);
        var currentRooms = await GetUserRoomIdsFromCacheAsync(userId, ct);

        var updatedRooms = new HashSet<Guid>(currentRooms);
        updatedRooms.UnionWith(roomIds);

        await hybridCache.SetAsync(key, updatedRooms, _cacheOptions, cancellationToken: ct);
    }

    private async Task RemoveUserRoomsBatchAsync(Guid userId, IEnumerable<Guid> roomIds, CancellationToken ct)
    {
        var key = UserRoomsKey(userId);
        var currentRooms = await GetUserRoomIdsFromCacheAsync(userId, ct);

        var updatedRooms = new HashSet<Guid>(currentRooms);
        updatedRooms.ExceptWith(roomIds);

        if (updatedRooms.Count > 0)
        {
            await hybridCache.SetAsync(key, updatedRooms, _cacheOptions, cancellationToken: ct);
        }
        else
        {
            await hybridCache.RemoveAsync(key, ct);
        }
    }

    #endregion
}