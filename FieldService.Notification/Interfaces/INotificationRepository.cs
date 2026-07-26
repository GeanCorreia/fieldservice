using System.Text.Json;

namespace FieldService.Notification.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyCollection<Guid>> GetPendingNotificationsForUser(
        Guid userId, 
        IEnumerable<Guid> tenantIds,
        DateTime? deliveredAfter = null);
    Task<DateTime?> GetLatestPendingNotificationCreatedAtForUser(
        Guid userId, 
        IEnumerable<Guid> tenantIds, 
        CancellationToken ct = default);
    Task Save(Entities.Notification notification, CancellationToken ct = default);
    Task<Entities.Notification?> GetById(Guid notificationId, CancellationToken ct = default);
    Task<JsonElement?> GetPayloadByMessageId(Guid notificationId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, JsonElement>> GetPayloadsByMessageIds(
        IReadOnlyCollection<Guid> notificationIds,
        CancellationToken ct = default);
    Task<bool> Exists(Guid notificationId, CancellationToken ct = default);
    Task<IReadOnlyCollection<Entities.NotificationDelivery>> GetNotificationDeliveries(Guid notificationId, CancellationToken ct = default);
}
