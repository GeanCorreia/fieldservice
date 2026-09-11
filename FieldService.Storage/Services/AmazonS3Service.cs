using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FieldService.Storage.Configuration;
using FieldService.Storage.Interfaces;
using Microsoft.Extensions.Options;

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
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = storagePath,
                InputStream = content,
                ContentType = contentType
            };

            await _client.PutObjectAsync(request, ct);
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

        using var response = await _client.GetObjectAsync(_bucketName, storagePath, ct);
        var memoryStream = new MemoryStream();

        await response.ResponseStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        await _client.DeleteObjectAsync(_bucketName, storagePath, ct);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string storagePath, string contentType, string hashMd5, TimeSpan expiry,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storagePath,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = contentType
        };

        return await Task.FromResult(_client.GetPreSignedURL(request));
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string storagePath, TimeSpan expiry, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storagePath,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return await Task.FromResult(_client.GetPreSignedURL(request));
    }
}