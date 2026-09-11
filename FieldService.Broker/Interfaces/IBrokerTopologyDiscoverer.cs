using FieldService.Broker.Message;

namespace FieldService.Broker.Interfaces;

public record BrokerTopology(
    string EntityName,
    IEnumerable<string> Subscriptions);

public interface IBrokerTopologyDiscoverer
{
    static abstract IEnumerable<BrokerTopology> DiscoverBrokerTopologies();
    static abstract IEnumerable<BrokerPublishContext> DiscoverPublishContexts();
    static abstract IEnumerable<BrokerSubscribeContext> DiscoverSubscriptionContexts();
}