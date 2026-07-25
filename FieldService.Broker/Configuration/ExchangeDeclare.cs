using RabbitMQ.Client;

namespace FieldService.Broker.Configuration;

public static class ExchangeDeclare
{
    public static void Execute(IModel channel, IEnumerable<ExchangeDeclaration> exchanges)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(exchanges);

        foreach (var exchange in exchanges)
        {
            if (string.IsNullOrWhiteSpace(exchange.Name))
                throw new InvalidOperationException("RabbitMQ exchange name cannot be empty.");

            channel.ExchangeDeclare(
                exchange: exchange.Name,
                type: exchange.Type,
                durable: exchange.Durable,
                autoDelete: exchange.AutoDelete,
                arguments: ToArguments(exchange.Arguments));
        }
    }

    private static IDictionary<string, object>? ToArguments(Dictionary<string, string>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
            return null;

        return arguments.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
    }
}

public sealed record ExchangeDeclaration(
    string Name,
    string Type = "direct",
    bool Durable = true,
    bool AutoDelete = false,
    Dictionary<string, string>? Arguments = null);
