using FieldService.Broker;
using FieldService.Broker.Message;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Broker.Messages;

public sealed class SignalRUserConnectedBrokerMessage
{
    public const string ExchangeName = "fieldservice.exchange";
    public const string QueueName = "signalr.user.connected.queue";
    public const string RoutingKey = "signalr.user.connected";
    public const string MessageTypeName = "signalr.user.connected";

    public SignalRUserConnectedBrokerMessage(
        SignalRConnectionContext payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        Payload = payload;
    }

    public SignalRConnectionContext Payload { get; }

    public BrokerMessage<SignalRConnectionContext> ToBrokerMessage()
        => new(
            MessageId: Guid.NewGuid(),
            TenantId: ResolveTenantId(Payload),
            TraceId: null,
            MessageType: MessageTypeName,
            CreatedAt: DateTimeService.GetNow(),
            SchemaVersion: new SchemaVersion(1, 0, 0),
            Payload: Payload,
            Context: new BrokerPublishContext(
                Exchange: ExchangeName,
                RoutingKey: RoutingKey));

    private static Guid ResolveTenantId(SignalRConnectionContext payload)
        => payload.TenantIds.FirstOrDefault(t => t != default);
}
