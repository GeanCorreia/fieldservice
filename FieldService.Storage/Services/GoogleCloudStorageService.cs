using FieldService.Storage.Configuration;
using FieldService.Storage.Interfaces;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace FieldService.Storage.Services;

internal class GoogleCloudStorageService : IStorageProviderService
{
    private readonly StorageClient _storageClient;
    private readonly UrlSigner _urlSigner;
    private readonly string _bucketName;

    public GoogleCloudStorageService(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storageOptions = options.Value ?? throw new InvalidOperationException("Storage options are not configured.");
        var googleOptions = storageOptions.GoogleCloudStorage;

        if (string.IsNullOrWhiteSpace(googleOptions.BucketName))
            throw new InvalidOperationException("Storage:GoogleCloudStorage:BucketName is not configured.");

        if (string.IsNullOrWhiteSpace(googleOptions.CredentialsFilePath))
            throw new InvalidOperationException("Storage:GoogleCloudStorage:CredentialsFilePath is not configured.");

        _bucketName = googleOptions.BucketName;
        _storageClient = StorageClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(googleOptions.CredentialsFilePath));
        _urlSigner = UrlSigner.FromCredentialFile(googleOptions.CredentialsFilePath);
    }

    public async Task UploadStreamAsync(string storagePath, Stream content, string contentType, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var originalPosition = content.CanSeek ? content.Position : 0;

        if (content.CanSeek)
            content.Position = 0;

        try
        {
            await _storageClient.UploadObjectAsync(
                _bucketName,
                storagePath,
                contentType,
                content,
                cancellationToken: ct);
        }
        finally
        {
            if (content.CanSeek)
                content.Position = originalPosition;
        }
    }

    public async Task<Stream> OpenReadStreamAsync(string storagePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var memoryStream = new MemoryStream();
        await _storageClient.DownloadObjectAsync(_bucketName, storagePath, memoryStream, cancellationToken: ct);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        await _storageClient.DeleteObjectAsync(_bucketName, storagePath, cancellationToken: ct);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string storagePath, string contentType, string hashMd5, TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var url = _urlSigner.Sign(
            _bucketName,
            storagePath,
            expiry,
            HttpMethod.Put);

        return await Task.FromResult(url);
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string storagePath, TimeSpan expiry, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var url = _urlSigner.Sign(
            _bucketName,
            storagePath,
            expiry,
            HttpMethod.Get);

        return await Task.FromResult(url);
    }
}