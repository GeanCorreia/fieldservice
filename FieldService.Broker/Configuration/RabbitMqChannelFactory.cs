using FieldService.Broker.Interfaces;
using RabbitMQ.Client;

namespace FieldService.Broker.Configuration;

internal sealed class RabbitMqChannelFactory(IRabbitMqPersistentConnection persistentConnection) : IRabbitMqChannelFactory
{
    public IModel CreateChannel()
    {
        var connection = persistentConnection.GetConnection();
        return connection.CreateModel();
    }
}
