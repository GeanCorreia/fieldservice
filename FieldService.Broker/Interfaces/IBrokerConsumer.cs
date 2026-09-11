using FieldService.Broker.Message;
using FieldService.Shared.Message;

namespace FieldService.Broker.Interfaces;

public interface IBrokerConsumer
{
    static abstract BrokerSubscribeContext BrokerSubscriptionContext { get; }
}

public interface IBrokerConsumer<TPayload> : IBrokerConsumer
    where TPayload : class, IMessagePayload
{
    Task ConsumeAsync(IMessage<TPayload> message, CancellationToken ct = default);
}

public interface IBrokerBatchConsumer<TPayload> : IBrokerConsumer
    where TPayload : class, IMessagePayload
{
    Task ConsumeAsync(IEnumerable<IMessage<TPayload>> messages, CancellationToken ct = default);
}