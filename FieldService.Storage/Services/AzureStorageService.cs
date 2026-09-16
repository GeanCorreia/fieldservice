using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
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
    
    private static byte[]? GetMd5BytesFromBase64(string? base64Hash)
    {
        if (string.IsNullOrWhiteSpace(base64Hash))
            return null;

        return Convert.FromBase64String(base64Hash);
    }

    public virtual async Task UploadStreamAsync(StoredFile file, Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        await EnsureContainerExistsAsync(ct);

        var originalPosition = content.CanSeek ? content.Position : 0;

        if (content.CanSeek)
            content.Position = 0;

        try
        {
            var blobClient = GetBlobClient(file.StoragePath);
            await blobClient.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType.ToString(),
                        ContentHash = GetMd5BytesFromBase64(file.HashMd5)
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

    public async Task<Stream> OpenReadStreamAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var blobClient = GetBlobClient(file.StoragePath);
        var stream = await blobClient.OpenReadAsync(cancellationToken: ct);
        return stream;
    }

    public async Task DeleteAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var blobClient = GetBlobClient(file.StoragePath);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }

    public virtual async Task<string> GeneratePresignedUploadUrlAsync(
        StoredFile file, 
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var blobClient = GetBlobClient(file.StoragePath);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Azure blob client cannot generate SAS URI with the configured credentials.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = file.StoragePath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry),
            Protocol = SasProtocol.HttpsAndHttp
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);
        
        sasBuilder.ContentType = file.ContentType.ToString();

        return await Task.FromResult(blobClient.GenerateSasUri(sasBuilder).ToString());
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(StoredFile file, TimeSpan expiry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var blobClient = GetBlobClient(file.StoragePath);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Azure blob client cannot generate SAS URI with the configured credentials.");

        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(expiry));

        return await Task.FromResult(sasUri.ToString());
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

            var exists = await GetBlobClient(file.StoragePath).ExistsAsync(ct);
            return (file, exists.Value);
        });

        return await Task.WhenAll(checks);
    }

    public async Task<Response<BlobProperties>> GetBlobPropertiesAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var blobClient = GetBlobClient(file.StoragePath);
        return await blobClient.GetPropertiesAsync(cancellationToken: ct);
    }
}