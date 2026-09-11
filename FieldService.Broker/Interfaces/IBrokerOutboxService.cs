using FieldService.Broker.Entities;
using FieldService.Broker.Message;

namespace FieldService.Broker.Interfaces;

public interface IBrokerOutboxService
{
    Task EnqueueAsync(
        BrokerOutbox brokerOutbox,
        CancellationToken ct = default);
}