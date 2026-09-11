namespace FieldService.Notification.Events;

public enum NotificationEventType
{
    Send = 1,
    Delivered = 2,
    Read = 3,
    Failed = 4,
    Canceled = 5
    
}

public enum NotificationFailureReason
{
    InvalidAddress = 1,      // E-mail inválido, número inexistente
    UserOffline = 2,         // WS desconectado
    ProviderError = 3,       // Falha no gateway (Twilio, SendGrid, FCM 500)
    RateLimitExceeded = 4,   // Limite de envio atingido
    Expired = 5,             // Notificação expirou antes de ser entregue
    Unknown = 99
}

public class NotificationEvent
{
    public Guid Id;
    public DateTimeOffset OccurredAt;
    public NotificationEventType Type;
    public Guid RecipientId ;
    public NotificationFailureReason? FailureReason;
    public Guid? CanceledByUserId;
    
    protected NotificationEvent() { }
    
    private NotificationEvent(
        Guid id,
        DateTimeOffset occurredAt,
        NotificationEventType type,
        Guid recipientId,
        NotificationFailureReason? failureReason = null,
        Guid? canceledByUserId = null)
    {
        Id = id;
        OccurredAt = occurredAt;
        Type = type;
        RecipientId = recipientId;
        FailureReason = failureReason;
        CanceledByUserId = canceledByUserId;
    }
    
    public static NotificationEvent CreateSendEvent(
        DateTimeOffset occurredAt,
        Guid recipientId,
        Guid? id = null)
    {
        return new NotificationEvent(
            id ?? Guid.NewGuid(),
            occurredAt,
            NotificationEventType.Send,
            recipientId);
    }
    
    public static NotificationEvent CreateDeliveredEvent(
        DateTimeOffset occurredAt,
        Guid recipientId,
        Guid? id = null)
    {
        return new NotificationEvent(
            id ?? Guid.NewGuid(),
            occurredAt,
            NotificationEventType.Delivered,
            recipientId);
    }

    public static NotificationEvent CreateReadEvent(
        DateTimeOffset occurredAt,
        Guid recipientId,
        Guid? id = null)
    {
        return new NotificationEvent(
            id ?? Guid.NewGuid(),
            occurredAt,
            NotificationEventType.Read,
            recipientId);
    }

    public static NotificationEvent CreateFailedEvent(
        DateTimeOffset occurredAt,
        Guid recipientId,
        NotificationFailureReason failureReason,
        Guid? id = null)
    {
        return new NotificationEvent(
            id ?? Guid.NewGuid(),
            occurredAt,
            NotificationEventType.Failed,
            recipientId,
            failureReason);
    }

    public static NotificationEvent CreateCanceledEvent(
        DateTimeOffset occurredAt,
        Guid recipientId,
        Guid canceledByUserId,
        Guid? id = null)
    {
        return new NotificationEvent(
            id ?? Guid.NewGuid(),
            occurredAt,
            NotificationEventType.Canceled,
            recipientId,
            null,
            canceledByUserId);
    }
}
