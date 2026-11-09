namespace FieldService.Cloud.Interfaces;

public interface IContainerProcessingLock
{
    Task<bool> AcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<bool> ReleaseAsync(string key, CancellationToken cancellationToken = default);
}
