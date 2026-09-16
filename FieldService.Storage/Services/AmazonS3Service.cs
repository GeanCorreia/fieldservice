using Azure;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using System.Net;

namespace FieldService.Storage.Services;

internal class AmazonS3Service : IStorageProviderService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    public AmazonS3Service(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storageOptions = options.Value ?? throw new InvalidOperationException("Storage options are not configured.");
        var amazonOptions = storageOptions.AmazonS3;

        if (string.IsNullOrWhiteSpace(amazonOptions.AccessKey))
            throw new InvalidOperationException("Storage:AmazonS3:AccessKey is not configured.");

        if (string.IsNullOrWhiteSpace(amazonOptions.SecretKey))
            throw new InvalidOperationException("Storage:AmazonS3:SecretKey is not configured.");

        if (string.IsNullOrWhiteSpace(amazonOptions.BucketName))
            throw new InvalidOperationException("Storage:AmazonS3:BucketName is not configured.");

        if (string.IsNullOrWhiteSpace(amazonOptions.Region))
            throw new InvalidOperationException("Storage:AmazonS3:Region is not configured.");

        _bucketName = amazonOptions.BucketName;

        var credentials = new BasicAWSCredentials(amazonOptions.AccessKey, amazonOptions.SecretKey);
        var clientConfig = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(amazonOptions.Region)
        };

        _client = new AmazonS3Client(credentials, clientConfig);
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
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = file.StoragePath,
                InputStream = content,
                ContentType = file.ContentType.ToString()
            };

            await _client.PutObjectAsync(request, ct);
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

        using var response = await _client.GetObjectAsync(_bucketName, file.StoragePath, ct);
        var memoryStream = new MemoryStream();

        await response.ResponseStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task DeleteAsync(StoredFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        await _client.DeleteObjectAsync(_bucketName, file.StoragePath, ct);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(StoredFile file, TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.ContentType.ToString());

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = file.StoragePath,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = file.ContentType.ToString()
        };

        if (!string.IsNullOrWhiteSpace(file.HashMd5))
        {
            request.Headers["Content-MD5"] = file.HashMd5;
        }

        return await Task.FromResult(_client.GetPreSignedURL(request));
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(StoredFile file, TimeSpan expiry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StoragePath);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = file.StoragePath,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return await Task.FromResult(_client.GetPreSignedURL(request));
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
                await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = file.StoragePath
                }, ct);

                return (file, true);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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