using FieldService.Broker.Message;

namespace FieldService.Broker.Interfaces;

public interface IMessageProducer
{
    Task PublishAsync<T>(BrokerMessage<T> brokerMessage);
}
