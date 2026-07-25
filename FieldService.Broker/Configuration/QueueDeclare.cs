using RabbitMQ.Client;

namespace FieldService.Broker.Configuration;

public static class QueueDeclare
{
    public static void Execute(IModel channel, IEnumerable<QueueDeclaration> queues)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(queues);

        foreach (var queue in queues)
        {
            if (string.IsNullOrWhiteSpace(queue.Name))
                throw new InvalidOperationException("RabbitMQ queue name cannot be empty.");

            channel.QueueDeclare(
                queue: queue.Name,
                durable: queue.Durable,
                exclusive: queue.Exclusive,
                autoDelete: queue.AutoDelete,
                arguments: ToArguments(queue.Arguments));
        }
    }

    private static IDictionary<string, object>? ToArguments(Dictionary<string, string>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
            return null;

        return arguments.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
    }
}

public sealed record QueueDeclaration(
    string Name,
    bool Durable = true,
    bool Exclusive = false,
    bool AutoDelete = false,
    Dictionary<string, string>? Arguments = null);
