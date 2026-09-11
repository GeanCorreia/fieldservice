using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

public interface IStoredFileOutboxRetryService
{
    Task OutboxAsync(
        IEnumerable<StoredFile> files,
        CancellationToken ct = default);
}

public interface IStoredFileOutboxService
{
    Task OutboxAsync(
        StoredFile file,
        CancellationToken ct = default);
}