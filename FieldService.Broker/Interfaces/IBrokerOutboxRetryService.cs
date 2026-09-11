using FieldService.Broker.Entities;

namespace FieldService.Broker.Interfaces;

public interface IBrokerOutboxRetryService
{
    Task OutboxAsync(
        IEnumerable<BrokerOutbox> messages,
        CancellationToken ct = default);
}