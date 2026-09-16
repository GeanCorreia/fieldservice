using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

public interface ICleanUpStoredFileRepository
{
    Task<IEnumerable<StoredFile>> GetByStatusAsync(
        StorageStatus status,
        DateTimeOffset createdBefore,
        CancellationToken ct = default);
}