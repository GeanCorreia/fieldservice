using System.Text.Json;
using FieldService.Broker;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Notification.Interfaces;
using FieldService.SignalR.Broker.Messages;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DomainVersion = FieldService.Shared.Types.Version;

namespace FieldService.Notification.Services;

internal sealed class NotificationPendingOnUserConnectedConsumer(
    IMessageConsumer messageConsumer,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscribeContext = new BrokerSubscribeContext(
            Exchange: SignalRUserConnectedBrokerMessage.ExchangeName,
            RoutingKey: SignalRUserConnectedBrokerMessage.RoutingKey,
            Queue: SignalRUserConnectedBrokerMessage.QueueName,
            AutoAck: false,
            PrefetchCount: 20);

        await messageConsumer.RegisterAsync<SignalRConnectionContext>(
            subscribeContext,
            HandleUserConnectedAsync,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUserConnectedAsync(
        BrokerMessage<SignalRConnectionContext> brokerMessage,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(brokerMessage);

        using var scope = scopeFactory.CreateScope();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var notificationCache = scope.ServiceProvider.GetRequiredService<INotificationCache>();
        var signalRMessageSender = scope.ServiceProvider.GetRequiredService<ISignalRMessageSender>();

        var connection = brokerMessage.Payload;
        var tenantIds = connection.TenantIds.Where(t => t != default).Distinct().ToArray();
        if (tenantIds.Length == 0)
            throw new InvalidOperationException("SignalR connection event must contain at least one tenant id.");

        var notificationIds = await notificationRepository.GetPendingNotificationsForUser(
            connection.UserId,
            tenantIds,
            deliveredAfter: null);

        var payloadsByMessageId = (await notificationCache.GetPayloadsByMessageIds(notificationIds, ct))
            .ToDictionary(x => x.Key, x => x.Value);
        var missingIds = notificationIds.Where(id => !payloadsByMessageId.ContainsKey(id)).ToArray();
        if (missingIds.Length > 0)
        {
            var payloadsFromDatabase = await notificationRepository.GetPayloadsByMessageIds(missingIds, ct);
            if (payloadsFromDatabase.Count > 0)
            {
                await notificationCache.SetPayloadsByMessageIds(payloadsFromDatabase, ct);
                foreach (var (messageId, payload) in payloadsFromDatabase)
                    payloadsByMessageId[messageId] = payload;
            }
        }

        foreach (var notificationId in notificationIds)
        {
            if (!payloadsByMessageId.TryGetValue(notificationId, out var payload))
                continue;

            var messageType = "notification.pending";
            var context = new SignalRTargetContext(
                SignalRTargetType.User,
                connection.UserId.ToString(),
                messageType,
                new DomainVersion(1, 0, 0));

            var message = new SignalRMessage<JsonElement>(
                MessageId: notificationId,
                TenantId: brokerMessage.TenantId,
                MessageType: messageType,
                CreatedAt: DateTime.UtcNow,
                CorrelationId: brokerMessage.CorrelationId,
                SchemaVersion: context.Version,
                Payload: payload.Clone(),
                Context: context);

            await signalRMessageSender.SendUserAsync(connection.UserId, message, ct);
        }
    }
}
