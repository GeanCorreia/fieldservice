using System.Text.Json;

namespace FieldService.Notification.Interfaces;

public interface INotificationCache
{
    Task SetPayloadByMessageId(Guid notificationId, JsonElement payload, CancellationToken ct = default);
    Task UpdatePayloadByMessageId(Guid notificationId, JsonElement payload, CancellationToken ct = default);
    Task<JsonElement?> GetPayloadByMessageId(Guid notificationId, CancellationToken ct = default);
    Task SetPayloadsByMessageIds(IReadOnlyDictionary<Guid, JsonElement> payloadsByMessageId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, JsonElement>> GetPayloadsByMessageIds(
        IReadOnlyCollection<Guid> notificationIds,
        CancellationToken ct = default);

    Task SetNotificationDelivery(
        Entities.NotificationDelivery delivery,
        DateTime notificationCreatedAt,
        CancellationToken ct = default);

    Task UpdateNotificationDelivery(
        Entities.NotificationDelivery delivery,
        DateTime notificationCreatedAt,
        CancellationToken ct = default);

    Task<Entities.NotificationDelivery?> GetNotificationDelivery(
        Guid notificationId,
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default);

    Task<IReadOnlyCollection<Guid>> GetNotificationIdsByUser(Guid userId, CancellationToken ct = default);
    Task<DateTime?> GetLastDeliveredNotificationCreatedAtByUser(Guid userId, CancellationToken ct = default);
}
