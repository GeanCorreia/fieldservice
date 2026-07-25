using RabbitMQ.Client;

namespace FieldService.Broker.Configuration;

public static class QueueBind
{
    public static void Execute(IModel channel, IEnumerable<BindingDeclaration> bindings)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(bindings);

        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Queue))
                throw new InvalidOperationException("RabbitMQ binding queue cannot be empty.");

            if (string.IsNullOrWhiteSpace(binding.Exchange))
                throw new InvalidOperationException("RabbitMQ binding exchange cannot be empty.");

            channel.QueueBind(
                queue: binding.Queue,
                exchange: binding.Exchange,
                routingKey: binding.RoutingKey,
                arguments: null);
        }
    }
}

public sealed record BindingDeclaration(
    string Queue,
    string Exchange,
    string RoutingKey = "");
