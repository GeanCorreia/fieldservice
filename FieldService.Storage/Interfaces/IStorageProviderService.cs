using Azure;
using Azure.Storage.Blobs.Models;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

public interface IStorageProviderService
{
    public Task UploadStreamAsync(
        StoredFile file,
        Stream content,
        CancellationToken ct = default);

    public Task<Stream> OpenReadStreamAsync(
        StoredFile file,
        CancellationToken ct = default);

    public Task DeleteAsync(
        StoredFile file,
        CancellationToken ct = default);

    public Task<string> GeneratePresignedUploadUrlAsync(
        StoredFile file,
        TimeSpan expiry,
        CancellationToken ct = default);

    public Task<string> GeneratePresignedDownloadUrlAsync(
        StoredFile file, 
        TimeSpan expiry,
        CancellationToken ct = default);
    
    public Task<IEnumerable<(StoredFile File, bool Exists)>> HasFilesAsync(
        IEnumerable<StoredFile> files,
        CancellationToken ct = default);

    public Task<Response<BlobProperties>> GetBlobPropertiesAsync(
        StoredFile file,
        CancellationToken ct = default);
}