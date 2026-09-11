using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FieldService.Storage.Configuration;
using FieldService.Storage.Interfaces;
using Microsoft.Extensions.Options;

namespace FieldService.Storage.Services;

internal class AzureStorageService : IStorageProviderService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public AzureStorageService(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storageOptions = options.Value ?? throw new InvalidOperationException("Storage options are not configured.");
        var azureOptions = storageOptions.AzureBlob;

        if (string.IsNullOrWhiteSpace(azureOptions.ConnectionString))
            throw new InvalidOperationException("Storage:AzureBlob:ConnectionString is not configured.");

        if (string.IsNullOrWhiteSpace(azureOptions.ContainerName))
            throw new InvalidOperationException("Storage:AzureBlob:ContainerName is not configured.");

        _blobServiceClient = new BlobServiceClient(azureOptions.ConnectionString);
        _containerName = azureOptions.ContainerName;
    }

    private BlobContainerClient ContainerClient => _blobServiceClient.GetBlobContainerClient(_containerName);

    private BlobClient GetBlobClient(string storagePath) => ContainerClient.GetBlobClient(storagePath);

    private async Task EnsureContainerExistsAsync(CancellationToken ct)
    {
        await ContainerClient.CreateIfNotExistsAsync(cancellationToken: ct);
    }

    public async Task UploadStreamAsync(string storagePath, Stream content, string contentType, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await EnsureContainerExistsAsync(ct);

        var originalPosition = content.CanSeek ? content.Position : 0;

        if (content.CanSeek)
            content.Position = 0;

        try
        {
            var blobClient = GetBlobClient(storagePath);
            await blobClient.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                },
                ct);
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

        var blobClient = GetBlobClient(storagePath);
        var stream = await blobClient.OpenReadAsync(cancellationToken: ct);
        return stream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var blobClient = GetBlobClient(storagePath);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string storagePath, string contentType, string hashMd5, TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var blobClient = GetBlobClient(storagePath);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Azure blob client cannot generate SAS URI with the configured credentials.");

        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Create | BlobSasPermissions.Write,
            DateTimeOffset.UtcNow.Add(expiry));

        return await Task.FromResult(sasUri.ToString());
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string storagePath, TimeSpan expiry, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var blobClient = GetBlobClient(storagePath);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Azure blob client cannot generate SAS URI with the configured credentials.");

        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(expiry));

        return await Task.FromResult(sasUri.ToString());
    }
}