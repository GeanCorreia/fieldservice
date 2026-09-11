using FieldService.Broker.Message;
using FieldService.Shared.Message;

namespace FieldService.Broker.Interfaces;

public interface IBrokerProducer
{
    static abstract BrokerPublishContext BrokerPublishContext { get; }
    
    BrokerPublishContext PublishContext { get; }
}

public interface IBrokerProducer<TPayload> : IBrokerProducer
    where TPayload : class, IMessagePayload
{
    Task PublishAsync(TPayload payload, CancellationToken ct = default);
}