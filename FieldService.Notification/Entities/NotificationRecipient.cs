using FieldService.Notification.Events;

namespace FieldService.Notification.Entities;

public class NotificationRecipient
{
    public Guid Id { get; private set; }
    public Guid NotificationId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public Guid? UserId { get; private set; }
    public string Address { get; private set; } = default!;
    
    private IList<NotificationEvent> _events = new List<NotificationEvent>();
    
    public IReadOnlyCollection<NotificationEvent> Events => _events.AsReadOnly();

    protected NotificationRecipient() { }

    public NotificationRecipient(Guid notificationId, NotificationChannel channel, string address, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        Id = Guid.NewGuid();
        NotificationId = notificationId;
        Channel = channel;
        UserId = userId;
        Address = address;
    }
    
    public void AddEvent(NotificationEvent @event)
    { 
        _events.Add(@event);
        
    }
}