using RabbitMQ.Client;

namespace FieldService.Broker;

public record BrokerContext(
    string Exchange,
    string RoutingKey);

public sealed record BrokerPublishContext(
    string Exchange,
    string RoutingKey,
    IBasicProperties? BasicProperties = null) : BrokerContext(Exchange, RoutingKey);

public sealed record BrokerSubscribeContext(
    string Exchange,
    string RoutingKey,
    string Queue,
    bool AutoAck = false,
    ushort PrefetchCount = 20,
    string? ConsumerTag = null,
    bool NoLocal = false,
    bool Exclusive = false,
    IDictionary<string, object>? ConsumerArguments = null,
    IDictionary<string, object>? BindingArguments = null) : BrokerContext(Exchange, RoutingKey);
