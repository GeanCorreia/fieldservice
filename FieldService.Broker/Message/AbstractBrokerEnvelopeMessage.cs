using System.Text.Json.Serialization;
using FieldService.Broker.Interfaces;
using FieldService.Shared.Message;

namespace FieldService.Broker.Message;

public abstract record AbstractBrokerEnvelopeMessage<TPayload> : IBrokerEnvelopeMessage<TPayload>
    where TPayload : class, IMessagePayload
{
    public IMessage<TPayload> Message { get; }
    public BrokerPublishContext PublishContext { get; }
    protected AbstractBrokerEnvelopeMessage() { }
    
    public AbstractBrokerEnvelopeMessage(IMessage<TPayload> message, BrokerPublishContext publishContext)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        PublishContext = publishContext ?? throw new ArgumentNullException(nameof(publishContext));
    }


    
}