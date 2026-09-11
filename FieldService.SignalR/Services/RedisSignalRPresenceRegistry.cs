using FieldService.Authentication.Interfaces;
using FieldService.Cache.Interfaces;
using FieldService.SignalR.Configuration;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace FieldService.SignalR.Services;

internal sealed class RedisSignalRPresenceRegistry(
    IRedisContext redisContext,
    ISessionCacheService sessionCacheService,
    IConfiguration configuration) : ISignalRPresenceRegistry
{
    private const string ConnectionPrefix = "signalr:conn:";
    private const string UserConnectionsPrefix = "signalr:user:";
    private const string SessionConnectionsPrefix = "signalr:session:";
    private const string TenantConnectionsPrefix = "signalr:tenant:";
    private const string UserTenantConnectionsPrefix = "signalr:user-tenant:";
    private const string AllConnectionsKey = "signalr:connections";


    // LUA SCRIPT: Registro Atômico + Limpeza Automática de Conexões Antigas da mesma Sessão
    private const string RegisterLuaScript = @"
        local connectionId = ARGV[1]
        local userId = ARGV[2]
        local sessionId = ARGV[3]
        local tenantId = ARGV[4]
        local ttlSeconds = tonumber(ARGV[5]) or 120

        local connKey = '" + ConnectionPrefix + @"' .. connectionId
        local userKey = '" + UserConnectionsPrefix + @"' .. userId
        local sessionKey = '" + SessionConnectionsPrefix + @"' .. sessionId
        local tenantKey = '" + TenantConnectionsPrefix + @"' .. tenantId
        local userTenantKey = '" + UserTenantConnectionsPrefix + @"' .. userId .. ':' .. tenantId
        local allKey = '" + AllConnectionsKey + @"'

        -- 1. Expulsa conexões antigas mapeadas para a mesma sessão
        local oldConns = redis.call('SMEMBERS', sessionKey)
        for _, oldConn in ipairs(oldConns) do
            if oldConn ~= connectionId then
                local oldConnKey = '" + ConnectionPrefix + @"' .. oldConn
                local oldData = redis.call('HMGET', oldConnKey, 'userId', 'tenantId')
                local oldUser = oldData[1]
                local oldTenant = oldData[2]

                if oldUser and oldTenant then
                    redis.call('SREM', '" + UserConnectionsPrefix + @"' .. oldUser, oldConn)
                    redis.call('SREM', '" + TenantConnectionsPrefix + @"' .. oldTenant, oldConn)
                    redis.call('SREM', '" + UserTenantConnectionsPrefix + @"' .. oldUser .. ':' .. oldTenant, oldConn)
                end

                redis.call('SREM', sessionKey, oldConn)
                redis.call('SREM', allKey, oldConn)
                redis.call('DEL', oldConnKey)
            end
        end

        -- 2. Insere os novos mapeamentos
        redis.call('SADD', userKey, connectionId)
        redis.call('SADD', sessionKey, connectionId)
        redis.call('SADD', tenantKey, connectionId)
        redis.call('SADD', userTenantKey, connectionId)
        redis.call('SADD', allKey, connectionId)

        redis.call('HSET', connKey, 'userId', userId, 'sessionId', sessionId, 'tenantId', tenantId)
        redis.call('EXPIRE', connKey, ttlSeconds)
        redis.call('EXPIRE', userKey, ttlSeconds)
        redis.call('EXPIRE', sessionKey, ttlSeconds)
        redis.call('EXPIRE', tenantKey, ttlSeconds)
        redis.call('EXPIRE', userTenantKey, ttlSeconds)
        redis.call('EXPIRE', allKey, ttlSeconds)
        return 1;
    ";

    // LUA SCRIPT: Remoção Atômica de Conexão
    private const string UnregisterLuaScript = @"
        local connectionId = ARGV[1]
        local connKey = '" + ConnectionPrefix + @"' .. connectionId
        local allKey = '" + AllConnectionsKey + @"'

        local data = redis.call('HMGET', connKey, 'userId', 'sessionId', 'tenantId')
        local userId = data[1]
        local sessionId = data[2]
        local tenantId = data[3]

        if not userId then
            return 0
        end

        redis.call('SREM', '" + UserConnectionsPrefix + @"' .. userId, connectionId)
        redis.call('SREM', '" + SessionConnectionsPrefix + @"' .. sessionId, connectionId)
        redis.call('SREM', '" + TenantConnectionsPrefix + @"' .. tenantId, connectionId)
        redis.call('SREM', '" + UserTenantConnectionsPrefix + @"' .. userId .. ':' .. tenantId, connectionId)
        redis.call('SREM', allKey, connectionId)
        redis.call('DEL', connKey)

        return 1;
    ";
    
    // LUA SCRIPT: Remove a conexão de todas as salas registradas no Redis
    private const string UnregisterFromAllRoomsLuaScript = @"
        local connectionId = ARGV[1]
        local userRoomsKey = 'signalr:conn-rooms:' .. connectionId

        -- 1. Busca todas as salas vinculadas a esta conexão
        local rooms = redis.call('SMEMBERS', userRoomsKey)

        -- 2. Remove a conexão de cada conjunto de sala
        for _, roomId in ipairs(rooms) do
            local roomKey = 'signalr:room:' .. roomId
            redis.call('SREM', roomKey, connectionId)
        end

        -- 3. Exclui o índice de salas da conexão
        redis.call('DEL', userRoomsKey)
        return 1;
    ";

    public async Task UnregisterFromAllRoomsAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        ct.ThrowIfCancellationRequested();

        await redisContext.Database.ScriptEvaluateAsync(
            UnregisterFromAllRoomsLuaScript,
            values: [connectionId]);
    }

    public async Task RegisterAsync(
        string connectionId,
        Guid userId,
        Guid sessionId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        ValidateRequiredParameters(connectionId, userId, sessionId, tenantId);
        ct.ThrowIfCancellationRequested();

        var session = await sessionCacheService.GetSessionAsync(sessionId, ct);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} is not active.");

        var remainingTtl = session.ExpiresAt.UtcDateTime - DateTime.UtcNow;
        var ttl = remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.Zero;

        var toleranceSeconds = configuration.GetValue<int>($"{SignalROptions.SectionName}:{nameof(SignalROptions.TtlToleranceInSeconds)}");
        var tolerance = TimeSpan.FromSeconds(toleranceSeconds);
        var expirationSeconds = (long)Math.Ceiling((ttl + tolerance).TotalSeconds);

        await redisContext.Database.ScriptEvaluateAsync(
            RegisterLuaScript,
            values:
            [
                connectionId,
                userId.ToString(),
                sessionId.ToString(),
                tenantId.ToString(),
                expirationSeconds.ToString()
            ]);
    }

    public async Task UnregisterAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        ct.ThrowIfCancellationRequested();

        await redisContext.Database.ScriptEvaluateAsync(
            UnregisterLuaScript,
            values: [connectionId]);
    }

    public async Task<IReadOnlyCollection<string>> GetUserConnectionsAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        var members = await redisContext.Database.SetMembersAsync(UserConnectionsKey(userId));
        return members.Select(m => m.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
    }

    public async Task<IReadOnlyCollection<SignalRConnectionContext>> GetUserRecipientsAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        return await ReadConnectionContextsByConnectionSetAsync(redisContext.Database, UserConnectionsKey(userId));
    }

    public async Task<SignalRConnectionContext?> GetSessionConnectionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();
        
        var contexts = await ReadConnectionContextsByConnectionSetAsync(redisContext.Database, SessionConnectionsKey(sessionId));
        return contexts.FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<SignalRConnectionContext>> GetTenantRecipientsAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ct.ThrowIfCancellationRequested();
        return await ReadConnectionContextsByConnectionSetAsync(redisContext.Database, TenantConnectionsKey(tenantId));
    }

    public async Task<IReadOnlyCollection<SignalRConnectionContext>> GetAllRecipientsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReadConnectionContextsByConnectionSetAsync(redisContext.Database, AllConnectionsKey);
    }

    private static async Task<IReadOnlyCollection<SignalRConnectionContext>> ReadConnectionContextsByConnectionSetAsync(IDatabase db, string setKey)
    {
        var members = await db.SetMembersAsync(setKey);
        return await ReadConnectionContextsByConnectionIdsAsync(
            db,
            members.Select(m => m.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)));
    }
    
    private static async Task<IReadOnlyCollection<SignalRConnectionContext>> ReadConnectionContextsByConnectionIdsAsync(
        IDatabase db,
        IEnumerable<string> connectionIds)
    {
        var uniqueConnectionIds = connectionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (uniqueConnectionIds.Length == 0)
            return Array.Empty<SignalRConnectionContext>();
        
        var fetchTasks = uniqueConnectionIds
            .Select(id => db.HashGetAllAsync(ConnectionKey(id)))
            .ToArray();

        var results = await Task.WhenAll(fetchTasks);

        var contexts = new List<SignalRConnectionContext>(uniqueConnectionIds.Length);

        for (var i = 0; i < uniqueConnectionIds.Length; i++)
        {
            var entry = results[i];
            if (entry.Length == 0)
                continue;

            var userId = ParseGuid(entry, "userId");
            var sessionId = ParseGuid(entry, "sessionId");
            var tenantId = ParseGuid(entry, "tenantId");

            if (userId.HasValue && sessionId.HasValue && tenantId.HasValue)
            {
                contexts.Add(new SignalRConnectionContext(
                    uniqueConnectionIds[i],
                    userId.Value,
                    sessionId.Value,
                    tenantId.Value,
                    DateTimeOffset.UtcNow));
            }
        }

        return contexts;
    }

    private static Guid? ParseGuid(HashEntry[] entries, string field)
    {
        foreach (var entry in entries)
        {
            if (entry.Name == field && Guid.TryParse(entry.Value, out var parsed))
                return parsed;
        }

        return null;
    }

    private static void ValidateRequiredParameters(string connectionId, Guid userId, Guid sessionId, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
    }

    private static string ConnectionKey(string connectionId) => $"{ConnectionPrefix}{connectionId}";
    private static string UserConnectionsKey(Guid userId) => $"{UserConnectionsPrefix}{userId}";
    private static string SessionConnectionsKey(Guid sessionId) => $"{SessionConnectionsPrefix}{sessionId}";
    private static string TenantConnectionsKey(Guid tenantId) => $"{TenantConnectionsPrefix}{tenantId}";
}