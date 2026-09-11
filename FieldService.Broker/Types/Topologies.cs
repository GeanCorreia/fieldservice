namespace FieldService.Broker.Types;

public sealed record ServiceBusEntityTopology(
    IReadOnlySet<string> Topics,
    IReadOnlyCollection<SubscriptionTopology> Subscriptions
);

public sealed record SubscriptionTopology(
    string TopicName,
    string SubscriptionName
);