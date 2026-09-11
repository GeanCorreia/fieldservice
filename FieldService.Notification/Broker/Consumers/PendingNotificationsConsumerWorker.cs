using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Shared.Message;
using FieldService.SignalR.Broker.Messages;
using FieldService.SignalR.Types;

namespace FieldService.Notification.Broker.Consumers;

public sealed class PendingNotificationsConsumerWorker
    : IBrokerConsumer<SignalRConnectionContext>
{
    private static string EntityName => SignalRUserConnectedBrokerEnvelopeMessage.EnvelopeContext.EntityName;
    private static string SubscriptionName => "notification.pending-user-connected.sub";
    public static BrokerSubscribeContext BrokerSubscriptionContext { get; } = new(
        EntityName: EntityName,
        SubscriptionName: SubscriptionName
    );

    public async Task ConsumeAsync(IMessage<SignalRConnectionContext> message, CancellationToken ct = default)
    {
        var payload = message.Payload;

        if (payload is null)
            throw new InvalidOperationException($"Mensagem inválida para {nameof(PendingNotificationsConsumerWorker)}.");

       

        Console.WriteLine("=================================================="); 
        Console.WriteLine($"[NotificationWorker] Mensagem Recebida! {DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")} UTC");
        Console.WriteLine($"UserId       : {payload.UserId}");
        Console.WriteLine($"SessionId    : {payload.SessionId}");
        Console.WriteLine($"TenantId     : {payload.TenantId}");
        Console.WriteLine("==================================================");

        await Task.Delay(100, ct);
    }
    
}