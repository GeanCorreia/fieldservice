using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Broker.Producers;
using FieldService.Data.Interfaces;
using FieldService.SignalR.Broker.Messages;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Services;

internal sealed class BrokerSignalRConnectionEventProducer : 
    AbstractBrokerProducer<SignalRUserConnectedBrokerEnvelopeMessage, SignalRConnectionContext> , ISignalRConnectionEventProducer
{
    public static BrokerPublishContext BrokerPublishContext =>  SignalRUserConnectedBrokerEnvelopeMessage.EnvelopeContext;
    public BrokerSignalRConnectionEventProducer(
        IBrokerPublisher brokerPublisher) 
        : base(brokerPublisher)
    {
    }

    protected override SignalRUserConnectedBrokerEnvelopeMessage CreateEnvelope(SignalRConnectionContext payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var message = new UserClientConnectionContextMessage(payload);
        return new SignalRUserConnectedBrokerEnvelopeMessage(message);
    }
}