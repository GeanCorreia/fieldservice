using FieldService.Notification.Entities;
using FieldService.Notification.Interfaces;
using FieldService.SignalR.Interfaces;
using Microsoft.Extensions.Logging;

namespace FieldService.Notification.Services;

public class WebsocketNotificationService : IWebsocketNotificationService
{
    private readonly ILogger<WebsocketNotificationService> _logger;
    private readonly IMessageSender _messageSender;
    
    public WebsocketNotificationService(
        ILogger<WebsocketNotificationService> logger, 
        IMessageSender messageSender)
    {
        _logger = logger;
        _messageSender = messageSender;
    }
    public Task SendNotification(Entities.Notification notification)
    {
        throw new NotImplementedException();
    }
    
    public Task<IEnumerable<NotificationRecipient>> GetRecipientsForNotification(Guid userId)
    {
        throw new NotImplementedException();
    }
}