namespace FieldService.Broker.Interfaces;

public interface IMessageProcessingLock
{
    Task<bool> BrokerConfiguratorAcquireLock(CancellationToken ct = default);
    Task<bool> BrokerConfiguratorReleaseLock(CancellationToken ct = default);
    Task<bool> MessageAcquireLock(Guid messageId, CancellationToken ct = default);
    Task<bool> MessageReleaseLock(Guid messageId, CancellationToken ct = default);
    
    Task<IEnumerable<Guid>> MessageAcquireLocks(IEnumerable<Guid> messageIds,  CancellationToken ct = default);
    Task<IEnumerable<Guid>> MessageReleaseLocks(IEnumerable<Guid> messageIds, CancellationToken ct = default);

}