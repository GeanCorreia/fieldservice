namespace FieldService.Storage.Interfaces;

internal interface IStoredFileProcessingLock
{

    Task<bool> StoredFileAcquireLock(Guid storedFileId, CancellationToken ct = default);
    Task<bool> StoredFileReleaseLock(Guid storedFileId, CancellationToken ct = default);
    
    Task<IEnumerable<Guid>> StoredFileAcquireLocks(IEnumerable<Guid> storedFileIds,  CancellationToken ct = default);
    Task<IEnumerable<Guid>> StoredFileReleaseLocks(IEnumerable<Guid> storedFileIds, CancellationToken ct = default);
    
}