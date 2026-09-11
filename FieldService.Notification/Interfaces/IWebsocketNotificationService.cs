namespace FieldService.Notification.Interfaces;

public interface IWebsocketNotificationService
{
    Task SendNotification(Entities.Notification notification);
}