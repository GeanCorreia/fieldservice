
using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

internal interface IStorageFallbackService
{
    Task CreateFallbackFailedUploadCache(Guid fileId, CancellationToken token);
    Task CreateFallbackCanceledUploadCache(Guid fileId, CancellationToken token);
    Task CreateFallbackSuccessUploadCache(Guid fileId, CancellationToken token);
    
    Task CreateFallbackCorruptedUploadCache(Guid fileId, CancellationToken token);
    Task RemoveFallbackCached(Guid fileId, CancellationToken token);

    Task<IEnumerable<StoredFile>> GetFailedUploadFallbackAsync(CancellationToken ct = default);
    Task<IEnumerable<StoredFile>> GetCanceledUploadFallbackAsync(CancellationToken ct = default);
    Task<IEnumerable<StoredFile>> GetSuccessUploadFallbackAsync(CancellationToken ct = default);
    Task<IEnumerable<StoredFile>> GetCorruptedUploadFallbackAsync(CancellationToken ct = default);
}   