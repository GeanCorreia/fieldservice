using System.Text.Json;
using FieldService.Shared.Message;
using FieldService.SignalR.Types;

namespace FieldService.Notification.Entities;

public class Notification
{
    private readonly List<NotificationDelivery> _deliveries = [];

    public Guid Id { get; private set; }
    public SignalRTargetContext TargetContext { get; private set; }
    public IReadOnlyCollection<NotificationDelivery> Deliveries => _deliveries.AsReadOnly();
    public JsonElement Payload { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public Notification(
        SignalRTargetContext targetContext,
        DateTime createdAt,
        JsonElement payload,
        IEnumerable<NotificationDelivery>? deliveries = null,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(targetContext);
        if (createdAt == default)
            throw new ArgumentException("CreatedAt must be informed.", nameof(createdAt));

        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt;
        TargetContext = targetContext;
        Payload = payload.Clone();

        if (deliveries is null)
            return;

        foreach (var delivery in deliveries)
        {
            ArgumentNullException.ThrowIfNull(delivery);
            if (delivery.NotificationId != Id)
                throw new InvalidOperationException("Delivery NotificationId must match Notification Id.");
            _deliveries.Add(delivery);
        }
    }

    public static Notification Create<TPayload>(Message<TPayload> message, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.SerializeToElement(message.Payload, options);

        return new Notification(
            message.TargetContext,
            message.CreatedAt,
            payload,
            deliveries: null,
            message.MessageId
        );
    }

    protected Notification()
    {
        TargetContext = null!;
        Payload = JsonDocument.Parse("{}").RootElement.Clone();
    }

    public bool IsDeleted() => DeletedAt.HasValue;

    public void RegisterDelivery(Guid userId, Guid deviceId, DateTime sentAt)
    {
        EnsureCanChangeDeliveries();

        var delivery = FindDeliveryByUser(userId, deviceId);
        if (delivery is not null)
            throw new InvalidOperationException("Delivery already registered.");

        delivery = new NotificationDelivery(Id, userId, deviceId, sentAt);
        _deliveries.Add(delivery);
    }

    public void MarkDelivered(Guid userId, Guid deviceId, DateTime deliveredAt)
    {
        EnsureCanChangeDeliveries();
        var delivery = FindDeliveryByUser(userId, deviceId);
        if (delivery is null)
            _deliveries.Add(new NotificationDelivery(Id, userId, deviceId, deliveredAt));
    }

    public void MarkRead(Guid userId, Guid deviceId, DateTime readAt)
    {
        EnsureCanChangeDeliveries();
        var delivery = GetRequiredDeliveryByUser(userId, deviceId);
        delivery.MarkRead(readAt);
    }

    private NotificationDelivery GetRequiredDeliveryByUser(Guid userId, Guid deviceId)
    {
        return FindDeliveryByUser(userId, deviceId)
               ?? throw new InvalidOperationException("Delivery was not found for this user.");
    }

    private NotificationDelivery? FindDeliveryByUser(Guid userId, Guid deviceId)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (deviceId == default)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        return _deliveries.FirstOrDefault(d => d.UserId == userId && d.DeviceId == deviceId);
    }

    public string ToJson(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }

    public static Notification FromJson(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Json is required.", nameof(json));

        return JsonSerializer.Deserialize<Notification>(json, options)
               ?? throw new InvalidOperationException("Notification could not be deserialized.");
    }

    public TPayload GetPayload<TPayload>(JsonSerializerOptions? options = null)
    {
        return Payload.Deserialize<TPayload>(options)
               ?? throw new InvalidOperationException("PayloadJson could not be deserialized.");
    }

    public void SetPayload(JsonElement payload)
    {
        Payload = payload.Clone();
    }

    public void MarkDeleted(DateTime deletedAt)
    {
        EnsureNotBeforeCreatedAt(deletedAt, nameof(deletedAt));
        EnsureCanMoveForward(DeletedAt, deletedAt, nameof(deletedAt));
        DeletedAt = deletedAt;
    }

    private void EnsureNotBeforeCreatedAt(DateTime value, string parameterName)
    {
        if (value < CreatedAt)
            throw new ArgumentException("Timestamp cannot be before CreatedAt.", parameterName);
    }

    private static void EnsureCanMoveForward(DateTime? currentValue, DateTime newValue, string parameterName)
    {
        if (currentValue.HasValue && newValue < currentValue.Value)
            throw new ArgumentException("Timestamp cannot move backwards.", parameterName);
    }

    

    private void EnsureCanChangeDeliveries()
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot change deliveries for a deleted notification.");
    }
}
