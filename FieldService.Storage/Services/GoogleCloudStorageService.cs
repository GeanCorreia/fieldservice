using Azure;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using Google.Cloud.Storage.V1;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using System.Net;

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

    public async Task UploadStreamAsync(StoredFile file, Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.ContentType.ToString());

        var originalPosition = content.CanSeek ? content.Position : 0;

        if (content.CanSeek)
            content.Position = 0;

        try
        {
            await _storageClient.UploadObjectAsync(
                _bucketName,
                file.StoragePath,
                file.ContentType.ToString(),
                content,
                cancellationToken: ct);
        }
        finally
        {
            if (content.CanSeek)
                content.Position = originalPosition;
        }
    }

    public async Task<Stream> OpenReadStreamAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var memoryStream = new MemoryStream();
        await _storageClient.DownloadObjectAsync(_bucketName, file.StoragePath, memoryStream, cancellationToken: ct);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task DeleteAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        await _storageClient.DeleteObjectAsync(_bucketName, file.StoragePath, cancellationToken: ct);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(StoredFile file, TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var requestHeaders = string.IsNullOrWhiteSpace(file.HashMd5)
            ? Array.Empty<KeyValuePair<string, IEnumerable<string>>>()
            : [new KeyValuePair<string, IEnumerable<string>>("Content-MD5", [file.HashMd5])];

        var requestTemplate = UrlSigner.RequestTemplate
            .FromBucket(_bucketName)
            .WithObjectName(file.StoragePath)
            .WithHttpMethod(HttpMethod.Put)
            .WithContentHeaders([
                new KeyValuePair<string, IEnumerable<string>>("Content-Type", [file.ContentType.ToString()])
            ])
            .WithRequestHeaders(requestHeaders);

        var url = _urlSigner.Sign(
            requestTemplate,
            UrlSigner.Options.FromDuration(expiry));

        return await Task.FromResult(url);
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(StoredFile file, TimeSpan expiry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var requestTemplate = UrlSigner.RequestTemplate
            .FromBucket(_bucketName)
            .WithObjectName(file.StoragePath)
            .WithHttpMethod(HttpMethod.Get);

        var url = _urlSigner.Sign(
            requestTemplate,
            UrlSigner.Options.FromDuration(expiry));

        return await Task.FromResult(url);
    }

    public async Task<IEnumerable<(StoredFile File, bool Exists)>> HasFilesAsync(IEnumerable<StoredFile> files, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var fileList = files as StoredFile[] ?? files.ToArray();
        if (fileList.Length == 0)
            return Array.Empty<(StoredFile File, bool Exists)>();

        var checks = fileList.Select(async file =>
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

            try
            {
                var obj = await _storageClient.GetObjectAsync(_bucketName, file.StoragePath, cancellationToken: ct);
                return (file, obj is not null);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return (file, false);
            }
        });

        return await Task.WhenAll(checks);
    }

    public Task<Response<BlobProperties>> GetBlobPropertiesAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        throw new NotSupportedException("Blob properties are only supported for Azure Blob storage.");
    }
}