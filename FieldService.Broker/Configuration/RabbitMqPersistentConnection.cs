using FieldService.Broker.Interfaces;
using RabbitMQ.Client;

namespace FieldService.Broker.Configuration;

internal sealed class RabbitMqPersistentConnection(ConnectionFactory connectionFactory)
    : IRabbitMqPersistentConnection, IDisposable
{
    private readonly object _syncRoot = new();
    private IConnection? _connection;

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_syncRoot)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _connection?.Dispose();
            _connection = connectionFactory.CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
