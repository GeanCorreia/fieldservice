namespace FieldService.Broker.Interfaces;

public interface IMessageProcessingLock
{
    Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);
    Task ReleaseAsync(string key, CancellationToken ct = default);
}
