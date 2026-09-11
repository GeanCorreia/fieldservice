using System.Text.Json.Serialization;
using FieldService.Broker.Message;
using FieldService.Shared.Message;

namespace FieldService.Broker.Interfaces;

public interface IBrokerEnvelopeMessage{
    [JsonIgnore]
    static BrokerPublishContext BrokerPublishContext { get; }
    IMessage Message { get; }
    BrokerPublishContext PublishContext { get; }
    
}

public interface IBrokerEnvelopeMessage<TPayload> : IBrokerEnvelopeMessage
    where TPayload : class, IMessagePayload
{
    new IMessage<TPayload> Message { get; }
    IMessage IBrokerEnvelopeMessage.Message => Message;
}
