namespace FieldService.Storage.Interfaces;

public interface IStorageProviderService
{
    public Task UploadStreamAsync(
        string storagePath,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    ;

    public Task<Stream> OpenReadStreamAsync(
        string storagePath,
        CancellationToken ct = default);

    public Task DeleteAsync(
        string storagePath,
        CancellationToken ct = default);

    public Task<string> GeneratePresignedUploadUrlAsync(
        string storagePath,
        string contentType,
        string hashMd5,
        TimeSpan expiry,
        CancellationToken ct = default);

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string storagePath, 
        TimeSpan expiry,
        CancellationToken ct = default);
}