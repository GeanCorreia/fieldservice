using System.Globalization;
using System.Text.Json;
using FieldService.Cache.Interfaces;
using FieldService.Notification.Configuration;
using FieldService.Notification.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FieldService.Notification.Services;

internal sealed class RedisNotificationCache(
    IRedisContext redisContext,
    IOptions<NotificationCacheOptions> options) : INotificationCache
{
    private const string NotificationPrefix = "notification:cache:notification:";
    private const string DeliveryPrefix = "notification:cache:delivery:";
    private const string UserNotificationPrefix = "notification:cache:user-notification:";
    private const string UserNotificationsPrefix = "notification:cache:user-notifications:";
    private const string UserLastDeliveredCreatedAtPrefix = "notification:cache:last-delivered-created-at:user:";
    private readonly TimeSpan cacheTtl = ResolveCacheTtl(options.Value);

    public Task SetPayloadByMessageId(Guid notificationId, JsonElement payload, CancellationToken ct = default)
    {
        return SavePayloadByMessageId(notificationId, payload, ct);
    }

    public Task UpdatePayloadByMessageId(Guid notificationId, JsonElement payload, CancellationToken ct = default)
    {
        return SavePayloadByMessageId(notificationId, payload, ct);
    }

    public async Task<JsonElement?> GetPayloadByMessageId(Guid notificationId, CancellationToken ct = default)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        ct.ThrowIfCancellationRequested();
        var payload = await redisContext.Database.StringGetAsync(NotificationKey(notificationId));
        if (!payload.HasValue)
            return null;

        return JsonDocument.Parse(payload!.ToString(), new JsonDocumentOptions()).RootElement.Clone();
    }

    public async Task SetPayloadsByMessageIds(
        IReadOnlyDictionary<Guid, JsonElement> payloadsByMessageId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payloadsByMessageId);
        ct.ThrowIfCancellationRequested();

        if (payloadsByMessageId.Count == 0)
            return;

        var batch = redisContext.Database.CreateBatch();
        var operations = new List<Task>(payloadsByMessageId.Count);

        foreach (var (notificationId, payload) in payloadsByMessageId)
        {
            if (notificationId == default)
                throw new ArgumentException("NotificationId is required.", nameof(payloadsByMessageId));

            operations.Add(batch.StringSetAsync(
                NotificationKey(notificationId),
                payload.GetRawText(),
                cacheTtl));
        }

        batch.Execute();
        await Task.WhenAll(operations);
    }

    public async Task<IReadOnlyDictionary<Guid, JsonElement>> GetPayloadsByMessageIds(
        IReadOnlyCollection<Guid> notificationIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notificationIds);
        ct.ThrowIfCancellationRequested();

        if (notificationIds.Count == 0)
            return new Dictionary<Guid, JsonElement>();

        var ids = notificationIds.Where(id => id != default).Distinct().ToArray();
        if (ids.Length == 0)
            throw new ArgumentException("At least one valid notification id is required.", nameof(notificationIds));

        var keys = ids.Select(id => (RedisKey)NotificationKey(id)).ToArray();
        var values = await redisContext.Database.StringGetAsync(keys);

        var result = new Dictionary<Guid, JsonElement>(ids.Length);
        for (var i = 0; i < ids.Length; i++)
        {
            var value = values[i];
            if (!value.HasValue)
                continue;

            result[ids[i]] = JsonDocument.Parse(value.ToString(), new JsonDocumentOptions()).RootElement.Clone();
        }

        return result;
    }

    public Task SetNotificationDelivery(
        Entities.NotificationDelivery delivery,
        DateTime notificationCreatedAt,
        CancellationToken ct = default)
    {
        return SaveNotificationDelivery(delivery, notificationCreatedAt, ct);
    }

    public Task UpdateNotificationDelivery(
        Entities.NotificationDelivery delivery,
        DateTime notificationCreatedAt,
        CancellationToken ct = default)
    {
        return SaveNotificationDelivery(delivery, notificationCreatedAt, ct);
    }

    public async Task<Entities.NotificationDelivery?> GetNotificationDelivery(
        Guid notificationId,
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        EnsureDeliveryKeyParts(notificationId, userId, deviceId);
        ct.ThrowIfCancellationRequested();

        var payload = await redisContext.Database.StringGetAsync(DeliveryKey(notificationId, userId, deviceId));
        if (!payload.HasValue)
            return null;

        return JsonSerializer.Deserialize<Entities.NotificationDelivery>(payload!)
               ?? throw new InvalidOperationException("NotificationDelivery could not be deserialized.");
    }

    public async Task<DateTime?> GetLastDeliveredNotificationCreatedAtByUser(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        var value = await redisContext.Database.StringGetAsync(UserLastDeliveredCreatedAtKey(userId));
        if (!value.HasValue)
            return null;

        if (!DateTime.TryParse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            throw new InvalidOperationException("Cached timestamp could not be parsed.");

        return parsed;
    }

    public async Task<IReadOnlyCollection<Guid>> GetNotificationIdsByUser(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();
        var members = await redisContext.Database.SetMembersAsync(UserNotificationsKey(userId));
        return members
            .Select(m => Guid.TryParse(m, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();
    }

    private async Task SavePayloadByMessageId(Guid notificationId, JsonElement payload, CancellationToken ct)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));
        ct.ThrowIfCancellationRequested();

        _ = await redisContext.Database.StringSetAsync(
            NotificationKey(notificationId),
            payload.GetRawText(),
            cacheTtl);
    }

    private static TimeSpan ResolveCacheTtl(NotificationCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.CacheTtlDays <= 0)
            throw new InvalidOperationException("Notification:CacheTtlDays must be greater than zero.");

        return TimeSpan.FromDays(options.CacheTtlDays);
    }

    private async Task SaveNotificationDelivery(
        Entities.NotificationDelivery delivery,
        DateTime notificationCreatedAt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (notificationCreatedAt == default)
            throw new ArgumentException("Notification created timestamp is required.", nameof(notificationCreatedAt));

        ct.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(delivery);
        _ = await redisContext.Database.StringSetAsync(
            DeliveryKey(delivery.NotificationId, delivery.UserId, delivery.DeviceId),
            payload,
            cacheTtl);

        _ = await redisContext.Database.StringSetAsync(
            UserNotificationKey(delivery.UserId, delivery.NotificationId),
            delivery.NotificationId.ToString(),
            cacheTtl);

        _ = await redisContext.Database.SetAddAsync(
            UserNotificationsKey(delivery.UserId),
            delivery.NotificationId.ToString());
        _ = await redisContext.Database.KeyExpireAsync(UserNotificationsKey(delivery.UserId), cacheTtl);

        _ = await redisContext.Database.StringSetAsync(
            UserLastDeliveredCreatedAtKey(delivery.UserId),
            notificationCreatedAt.ToString("O", CultureInfo.InvariantCulture),
            cacheTtl);
    }

    private static void EnsureDeliveryKeyParts(Guid notificationId, Guid userId, Guid deviceId)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (deviceId == default)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
    }

    private static string NotificationKey(Guid notificationId) => $"{NotificationPrefix}{notificationId}";
    private static string DeliveryKey(Guid notificationId, Guid userId, Guid deviceId) =>
        $"{DeliveryPrefix}{notificationId}:{userId}:{deviceId}";
    private static string UserNotificationKey(Guid userId, Guid notificationId) =>
        $"{UserNotificationPrefix}{userId}:{notificationId}";
    private static string UserNotificationsKey(Guid userId) =>
        $"{UserNotificationsPrefix}{userId}";

    private static string UserLastDeliveredCreatedAtKey(Guid userId) =>
        $"{UserLastDeliveredCreatedAtPrefix}{userId}";
}
