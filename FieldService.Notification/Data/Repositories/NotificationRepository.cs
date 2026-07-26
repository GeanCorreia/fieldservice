using System.Text.Json;
using FieldService.Data.Interfaces;
using FieldService.Notification.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Notification.Data.Repositories;

internal sealed class NotificationRepository(
    NotificationDbContext dbContext,
    ISqlUnitOfWork<NotificationDbContext> unitOfWork) : INotificationRepository
{
    public async Task<IReadOnlyCollection<Guid>> GetPendingNotificationsForUser(
        Guid userId,
        IEnumerable<Guid> tenantIds,
        DateTime? deliveredAfter = null)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(tenantIds);

        var notificationsQuery = BuildPendingNotificationsQuery(userId, tenantIds);

        if (deliveredAfter.HasValue)
            notificationsQuery = notificationsQuery.Where(n => n.CreatedAt > deliveredAfter.Value);

        return await notificationsQuery
            .OrderBy(n => n.CreatedAt)
            .Select(n => n.Id)
            .ToArrayAsync();
    }

    public async Task<DateTime?> GetLatestPendingNotificationCreatedAtForUser(
        Guid userId,
        IEnumerable<Guid> tenantIds,
        CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(tenantIds);

        return await BuildPendingNotificationsQuery(userId, tenantIds)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => (DateTime?)n.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task Save(Entities.Notification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await dbContext.Notifications.AnyAsync(n => n.Id == notification.Id, ct))
        {
            dbContext.Notifications.Update(notification);
        }
        else
        {
            await dbContext.Notifications.AddAsync(notification, ct);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<Entities.Notification?> GetById(Guid notificationId, CancellationToken ct = default)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        return await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, ct);
    }

    public async Task<JsonElement?> GetPayloadByMessageId(Guid notificationId, CancellationToken ct = default)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        return await dbContext.Notifications
            .Where(n => n.Id == notificationId && n.DeletedAt == null)
            .Select(n => (JsonElement?)n.Payload)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, JsonElement>> GetPayloadsByMessageIds(
        IReadOnlyCollection<Guid> notificationIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notificationIds);
        if (notificationIds.Count == 0)
            return new Dictionary<Guid, JsonElement>();

        var ids = notificationIds.Where(id => id != default).Distinct().ToArray();
        if (ids.Length == 0)
            throw new ArgumentException("At least one valid notification id is required.", nameof(notificationIds));

        var payloads = await dbContext.Notifications
            .Where(n => n.DeletedAt == null && ids.Contains(n.Id))
            .Select(n => new { n.Id, n.Payload })
            .ToListAsync(ct);

        return payloads.ToDictionary(x => x.Id, x => x.Payload);
    }

    public async Task<bool> Exists(Guid notificationId, CancellationToken ct = default)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        return await dbContext.Notifications.AnyAsync(n => n.Id == notificationId, ct);
    }

    public async Task<IReadOnlyCollection<Entities.NotificationDelivery>> GetNotificationDeliveries(Guid notificationId, CancellationToken ct = default)
    {
        if (notificationId == default)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));

        return await dbContext.NotificationDeliveries
            .Where(d => d.NotificationId == notificationId)
            .ToArrayAsync(ct);
    }

    private IQueryable<Entities.Notification> BuildPendingNotificationsQuery(
        Guid userId,
        IEnumerable<Guid> tenantIds)
    {
        var allowedTenantIds = tenantIds
            .Where(t => t != default)
            .Distinct()
            .ToHashSet();
        var allowedTenantIdStrings = allowedTenantIds
            .Select(t => t.ToString())
            .ToArray();
        var userIdString = userId.ToString();

        if (allowedTenantIds.Count == 0)
            throw new ArgumentException("At least one valid tenantId is required.", nameof(tenantIds));

        return dbContext.Notifications
            .Where(n => n.DeletedAt == null)
            .Where(n =>
                n.TargetContext.TargetType == SignalRTargetType.All ||
                (n.TargetContext.TargetType == SignalRTargetType.User &&
                 n.TargetContext.TargetId == userIdString) ||
                (n.TargetContext.TargetType == SignalRTargetType.Tenant &&
                 n.TargetContext.TargetId != null &&
                 allowedTenantIdStrings.Contains(n.TargetContext.TargetId)))
            .Where(n => !dbContext.NotificationDeliveries
                .Any(d => d.UserId == userId && d.NotificationId == n.Id));
    }

}
