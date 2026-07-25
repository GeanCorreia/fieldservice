using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace FieldService.Broker.Configuration;

public static class BrokerConfiguration
{
    public static string GetRabbitMqConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RabbitMQ")
                               ?? throw new InvalidOperationException("ConnectionStrings:RabbitMQ is not configured.");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:RabbitMQ is empty.");

        return connectionString;
    }

    public static ConnectionFactory CreateConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string for RabbitMQ is empty.");

        return new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }
}
