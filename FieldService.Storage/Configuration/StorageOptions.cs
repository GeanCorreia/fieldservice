using FieldService.Storage.Entities;

namespace FieldService.Storage.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public const string AzureSectionName = "AzureBlob";
    public const string LocalSectionName = "LocalStorage";

    public const string AmazonS3SectionName = "AmazonS3";
    public const string GoogleCloudStorageSectionName = "GoogleCloudStorage";

    public StorageProvider DefaultStorageProvider { get; init; }
    public int ExpiryPreSignedUrlMinutes { get; init; } = 5;
    public int StoredFileLockTtlSeconds { get; init; } = 30;
    public int MaxParallelism { get; init; } = 5;
    public string LocalStorageBasePath { get; init; } = "Storage";
    
    public int TtlFallBackHours { get; init; } = 24;
    public int TtlCacheMinutes { get; init; } = 5;
    
    public AzureBlobOptions AzureBlob { get; init; } = new();
    public LocalStorageOptions LocalStorage { get; init; } = new();
    public AmazonS3Options AmazonS3 { get; init; } = new();
    public GoogleCloudStorageOptions GoogleCloudStorage { get; init; } = new();
    
    
}

public sealed class AzureBlobOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
}

public sealed class LocalStorageOptions
{
    public string BasePath { get; init; } = "Storage";
}

public sealed class AmazonS3Options
{
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
}

public sealed class GoogleCloudStorageOptions
{
    public string ProjectId { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string CredentialsFilePath { get; init; } = string.Empty;
}