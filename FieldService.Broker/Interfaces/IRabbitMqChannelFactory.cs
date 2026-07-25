using RabbitMQ.Client;

namespace FieldService.Broker.Interfaces;

public interface IRabbitMqChannelFactory
{
    IModel CreateChannel();
}
