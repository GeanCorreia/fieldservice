using RabbitMQ.Client;

namespace FieldService.Broker.Interfaces;

public interface IRabbitMqPersistentConnection
{
    IConnection GetConnection();
}
