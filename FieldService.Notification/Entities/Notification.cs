using System.Text.Json;
using FieldService.Notification.Events;
using FieldService.Shared.Message;

namespace FieldService.Notification.Entities;

public record NotificationAddress(
    NotificationChannel  Channel,
    string Address,
    Guid? UserId = null);

public record NotificationFailed(
    string Address,
    DateTimeOffset OccurredAt,
    NotificationFailureReason Reason);

public record NotificationRead(
    string Address,
    DateTimeOffset OccurredAt);

public record NotificationDelivered(
    string Address,
    DateTimeOffset OccurredAt);

public enum NotificationChannel
{
    Websocket = 1,
    Email = 2,
    Sms = 3,
    WhatsApp = 4,
    PushNotification = 5
}
public class Notification
{
    public Guid Id { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public JsonElement Message { get; private set; } = default!;
    public JsonElement Payload => Message.GetProperty("Payload");
    private ICollection<NotificationRecipient> _recipients = new List<NotificationRecipient>();
    public IReadOnlyCollection<NotificationRecipient> Recipients => _recipients.ToList().AsReadOnly();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    protected Notification()
    {
    }

    private Notification(
        Guid id,
        Guid createdByUserId,
        JsonElement message,
        IReadOnlyCollection<NotificationRecipient> recipients,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt)
    {
        if (id == default)
            throw new ArgumentException("Id is required.", nameof(id));
        if (createdByUserId == default)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
        ArgumentNullException.ThrowIfNull(message);
        if (recipients.Count == 0)
            throw new ArgumentException("At least one recipient is required.", nameof(recipients));
        if (expiresAt.HasValue && expiresAt.Value < createdAt)
            throw new ArgumentException("ExpiresAt cannot be earlier than CreatedAt.", nameof(expiresAt));

        Id = id;
        CreatedByUserId = createdByUserId;
        Message = message;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        _recipients = recipients.ToList();
    }

    public static Notification Create(
        Guid createdByUserId,
        IEnumerable<NotificationAddress> addresses,
        JsonElement message,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        ArgumentNullException.ThrowIfNull(message);

        var notificationId = id ?? Guid.NewGuid();
        var recipients = addresses
            .Select(address => new NotificationRecipient(notificationId,  address.Channel, address.Address, address.UserId))
            .ToList();

        return new Notification(
            notificationId,
            createdByUserId,
            message,
            recipients,
            createdAt,
            expiresAt);
    }


    public void Cancel(Guid userId, DateTimeOffset canceledAt)
    {
        foreach (var recipient in Recipients)
        {
            try
            {
                CancelRecipient(recipient.Address, canceledAt, userId);
            }
            catch
            {
            }
        }
    }
    public void CancelRecipient(string address, DateTimeOffset canceledAt, Guid canceledByUserId)
    {
        var recipient = _recipients.FirstOrDefault(r => r.Address == address);
        if (recipient == null)
        {
            throw new InvalidOperationException($"Recipient {address} not found");
            
        }
        if (canceledAt < CreatedAt)
        {
            throw new InvalidOperationException("CanceledAt cannot be earlier than CreatedAt");
        }

        if (ExpiresAt.HasValue && canceledAt > ExpiresAt.Value)
        {
            throw new InvalidOperationException("CanceledAt cannot be later than ExpiresAt");
        }
        
        var wasNotified = WasNotified(address);
        if (wasNotified)
        {
            throw new InvalidOperationException($"Cannot remove recipient {address} because they have already been notified");
        }
        
        var canceledEvent = NotificationEvent.CreateCanceledEvent(canceledAt, recipient.Id, canceledByUserId);
        recipient.AddEvent(canceledEvent);
    }
    
    private bool WasNotified(string address)
    {
        var recipient = _recipients.FirstOrDefault(r => r.Address == address);
        return recipient is not null &&
               recipient.Events.Any(e => e.Type == NotificationEventType.Delivered || e.Type == NotificationEventType.Read);
    }
    
    public IEnumerable<NotificationRecipient> GetUnreadRecipients()
    {
        
        return Recipients.Where(recipient => 
            recipient.Channel != NotificationChannel.Sms && 
            recipient.Events.All(@event => @event.Type != NotificationEventType.Read));
    }
    
    public IEnumerable<NotificationFailed> GetFailedRecipients()
    {
        return Recipients
            .Select(recipient => new
            {
                Recipient = recipient,
                LastEvent = recipient.Events.OrderByDescending(e => e.OccurredAt).FirstOrDefault()
            })
            .Where(x => x.LastEvent is not null && x.LastEvent.Type == NotificationEventType.Failed)
            .Select(x => new NotificationFailed(
                x.Recipient.Address,
                x.LastEvent!.OccurredAt,
                x.LastEvent.FailureReason ?? NotificationFailureReason.Unknown
            ));
    }
    
    public void CancelFailedRecipients(Guid userId,  DateTimeOffset canceledAt)
    {
        var failedRecipients = GetFailedRecipients();
        foreach (var recipient in failedRecipients)
        {
            CancelRecipient(recipient.Address, canceledAt, userId);
        }
    }

    public IEnumerable<NotificationRead> GetReadRecipients()
    {
        return Recipients
            .Select(recipient => new
            {
                Recipient = recipient,
                ReadEvent = recipient.Events.FirstOrDefault(e => e.Type == NotificationEventType.Read)
            })
            .Where(x => x.ReadEvent is not null)
            .Select(x => new NotificationRead(
                x.Recipient.Address,
                x.ReadEvent!.OccurredAt
            ));
    }

    public IEnumerable<NotificationDelivered> GetDeliveredRecipients()
    {
        return Recipients
            .Select(recipient => new
            {
                Recipient = recipient,
                DeliveredEvent = recipient.Events
                    .Where(e => e.Type == NotificationEventType.Delivered)
                    .OrderByDescending(e => e.OccurredAt)
                    .FirstOrDefault(),
                HasReadEvent = recipient.Events.Any(e => e.Type == NotificationEventType.Read),
                HasFailedEvent = recipient.Events.Any(e => e.Type == NotificationEventType.Failed),
                HasCanceledEvent = recipient.Events.Any(e => e.Type == NotificationEventType.Canceled)
            })
            .Where(x => x.DeliveredEvent is not null &&
                        !x.HasReadEvent &&
                        !x.HasFailedEvent &&
                        !x.HasCanceledEvent)
            .Select(x => new NotificationDelivered(
                x.Recipient.Address,
                x.DeliveredEvent!.OccurredAt
            ));
    }
    
}
