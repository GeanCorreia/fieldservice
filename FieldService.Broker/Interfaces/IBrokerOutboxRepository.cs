using FieldService.Broker.Entities;

namespace FieldService.Broker.Interfaces;

public interface IBrokerOutboxRepository
{
    Task<BrokerOutbox?> GetByIdAsync(
        Guid messageId, 
        CancellationToken ct = default);
    Task<IEnumerable<BrokerOutbox>> GetPendingAsync(
        TimeSpan interval, 
        int? maxCount = null, 
        CancellationToken ct = default);
    
    Task SaveAsync(BrokerOutbox brokerOutbox, CancellationToken ct = default);
    Task SaveAsync(IEnumerable<BrokerOutbox> brokerOutboxes, CancellationToken ct = default);
}