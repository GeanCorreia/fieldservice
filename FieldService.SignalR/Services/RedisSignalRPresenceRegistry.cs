using FieldService.Cache.Interfaces;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using StackExchange.Redis;

namespace FieldService.SignalR.Services;

internal sealed class RedisSignalRPresenceRegistry(IRedisContext redisContext) : ISignalRPresenceRegistry
{
    private const string ConnectionPrefix = "signalr:conn:";
    private const string ConnectionTenantsPrefix = "signalr:conn-tenants:";
    private const string UserConnectionsPrefix = "signalr:user:";
    private const string DeviceConnectionsPrefix = "signalr:device:";
    private const string TenantConnectionsPrefix = "signalr:tenant:";
    private const string UserTenantsPrefix = "signalr:user-tenants:";
    private const string UserTenantConnectionsPrefix = "signalr:user-tenant:";
    private const string AllConnectionsKey = "signalr:connections";

    public async Task RegisterAsync(string connectionId, Guid userId, Guid deviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (deviceId == default)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var tx = db.CreateTransaction();
        _ = tx.SetAddAsync(UserConnectionsKey(userId), connectionId);
        _ = tx.SetAddAsync(DeviceConnectionsKey(deviceId), connectionId);
        _ = tx.SetAddAsync(AllConnectionsKey, connectionId);
        _ = tx.HashSetAsync(ConnectionKey(connectionId), ToEntry(userId, deviceId));

        var executed = await tx.ExecuteAsync();
        if (!executed)
            throw new InvalidOperationException("Could not register SignalR presence in Redis.");
    }

    public async Task UnregisterAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var connectionData = await db.HashGetAllAsync(ConnectionKey(connectionId));
        if (connectionData.Length == 0)
            return;

        var userId = ParseGuid(connectionData, "userId");
        var deviceId = ParseGuid(connectionData, "deviceId");
        var tenantIds = await ReadGuidsAsync(db, ConnectionTenantsKey(connectionId));

        var tx = db.CreateTransaction();
        if (userId.HasValue)
            _ = tx.SetRemoveAsync(UserConnectionsKey(userId.Value), connectionId);
        if (deviceId.HasValue)
            _ = tx.SetRemoveAsync(DeviceConnectionsKey(deviceId.Value), connectionId);
        _ = tx.SetRemoveAsync(AllConnectionsKey, connectionId);

        foreach (var tenantId in tenantIds)
        {
            _ = tx.SetRemoveAsync(TenantConnectionsKey(tenantId), connectionId);
            if (userId.HasValue)
                _ = tx.SetRemoveAsync(UserTenantConnectionsKey(userId.Value, tenantId), connectionId);
        }

        _ = tx.KeyDeleteAsync(ConnectionTenantsKey(connectionId));
        _ = tx.KeyDeleteAsync(ConnectionKey(connectionId));

        var executed = await tx.ExecuteAsync();
        if (!executed)
            throw new InvalidOperationException("Could not unregister SignalR presence in Redis.");

        if (userId.HasValue)
            await CleanupUserTenantIndexAsync(db, userId.Value, tenantIds);
    }

    public async Task SubscribeTenantAsync(string connectionId, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var connectionData = await db.HashGetAllAsync(ConnectionKey(connectionId));
        if (connectionData.Length == 0)
            throw new InvalidOperationException("Connection was not found in presence registry.");

        var userId = ParseGuid(connectionData, "userId");
        var tx = db.CreateTransaction();
        _ = tx.SetAddAsync(ConnectionTenantsKey(connectionId), tenantId.ToString());
        _ = tx.SetAddAsync(TenantConnectionsKey(tenantId), connectionId);
        if (userId.HasValue)
        {
            _ = tx.SetAddAsync(UserTenantsKey(userId.Value), tenantId.ToString());
            _ = tx.SetAddAsync(UserTenantConnectionsKey(userId.Value, tenantId), connectionId);
        }

        var executed = await tx.ExecuteAsync();
        if (!executed)
            throw new InvalidOperationException("Could not subscribe tenant in presence registry.");
    }

    public async Task UnsubscribeTenantAsync(string connectionId, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var connectionData = await db.HashGetAllAsync(ConnectionKey(connectionId));
        if (connectionData.Length == 0)
            return;

        var userId = ParseGuid(connectionData, "userId");
        var tx = db.CreateTransaction();
        _ = tx.SetRemoveAsync(ConnectionTenantsKey(connectionId), tenantId.ToString());
        _ = tx.SetRemoveAsync(TenantConnectionsKey(tenantId), connectionId);
        if (userId.HasValue)
            _ = tx.SetRemoveAsync(UserTenantConnectionsKey(userId.Value, tenantId), connectionId);

        var executed = await tx.ExecuteAsync();
        if (!executed)
            throw new InvalidOperationException("Could not unsubscribe tenant in presence registry.");

        if (userId.HasValue)
            await CleanupUserTenantIndexAsync(db, userId.Value, [tenantId]);
    }

    public async Task<IReadOnlyCollection<Guid>> GetUserTenantsAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        return await ReadGuidsAsync(redisContext.Database, UserTenantsKey(userId));
    }

    public async Task<IReadOnlyCollection<string>> GetUserConnectionsAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        var members = await redisContext.Database.SetMembersAsync(UserConnectionsKey(userId));
        return members.Select(m => m.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
    }

    public async Task<IReadOnlyCollection<SignalRRecipient>> GetUserRecipientsAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        return await ReadRecipientsByConnectionSetAsync(redisContext.Database, UserConnectionsKey(userId));
    }

    public async Task<IReadOnlyCollection<SignalRRecipient>> GetDeviceRecipientsAsync(Guid deviceId, CancellationToken ct = default)
    {
        if (deviceId == default)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        ct.ThrowIfCancellationRequested();
        return await ReadRecipientsByConnectionSetAsync(redisContext.Database, DeviceConnectionsKey(deviceId));
    }

    public async Task<IReadOnlyCollection<SignalRRecipient>> GetTenantRecipientsAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ct.ThrowIfCancellationRequested();
        return await ReadRecipientsByConnectionSetAsync(redisContext.Database, TenantConnectionsKey(tenantId));
    }

    public async Task<IReadOnlyCollection<SignalRRecipient>> GetAllRecipientsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await ReadRecipientsByConnectionSetAsync(redisContext.Database, AllConnectionsKey);
    }

    private static async Task<IReadOnlyCollection<Guid>> ReadGuidsAsync(IDatabase db, string key)
    {
        var members = await db.SetMembersAsync(key);
        return members
            .Select(m => Guid.TryParse(m, out var guid) ? guid : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();
    }

    private static async Task<IReadOnlyCollection<SignalRRecipient>> ReadRecipientsByConnectionSetAsync(IDatabase db, string setKey)
    {
        var members = await db.SetMembersAsync(setKey);
        return await ReadRecipientsByConnectionIdsAsync(
            db,
            members.Select(m => m.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    private static async Task<IReadOnlyCollection<SignalRRecipient>> ReadRecipientsByConnectionIdsAsync(
        IDatabase db,
        IEnumerable<string> connectionIds)
    {
        var recipients = new HashSet<SignalRRecipient>();
        foreach (var connectionId in connectionIds)
        {
            var entry = await db.HashGetAllAsync(ConnectionKey(connectionId));
            if (entry.Length == 0)
                continue;

            var userId = ParseGuid(entry, "userId");
            var deviceId = ParseGuid(entry, "deviceId");
            if (userId.HasValue && deviceId.HasValue)
                recipients.Add(new SignalRRecipient(userId.Value, deviceId.Value));
        }

        return recipients.ToArray();
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

    private static async Task CleanupUserTenantIndexAsync(IDatabase db, Guid userId, IEnumerable<Guid> tenantIds)
    {
        foreach (var tenantId in tenantIds)
        {
            var remaining = await db.SetLengthAsync(UserTenantConnectionsKey(userId, tenantId));
            if (remaining == 0)
            {
                _ = db.SetRemoveAsync(UserTenantsKey(userId), tenantId.ToString());
                _ = db.KeyDeleteAsync(UserTenantConnectionsKey(userId, tenantId));
            }
        }
    }

    private static HashEntry[] ToEntry(Guid userId, Guid deviceId)
    {
        return
        [
            new HashEntry("userId", userId.ToString()),
            new HashEntry("deviceId", deviceId.ToString())
        ];
    }

    private static string ConnectionKey(string connectionId) => $"{ConnectionPrefix}{connectionId}";
    private static string ConnectionTenantsKey(string connectionId) => $"{ConnectionTenantsPrefix}{connectionId}";
    private static string UserConnectionsKey(Guid userId) => $"{UserConnectionsPrefix}{userId}";
    private static string DeviceConnectionsKey(Guid deviceId) => $"{DeviceConnectionsPrefix}{deviceId}";
    private static string TenantConnectionsKey(Guid tenantId) => $"{TenantConnectionsPrefix}{tenantId}";
    private static string UserTenantsKey(Guid userId) => $"{UserTenantsPrefix}{userId}";
    private static string UserTenantConnectionsKey(Guid userId, Guid tenantId) => $"{UserTenantConnectionsPrefix}{userId}:{tenantId}";
}
