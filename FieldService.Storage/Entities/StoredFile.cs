using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.StaticFiles;

namespace FieldService.Storage.Entities;

public enum StorageProvider
{
    AzureBlob = 1,
    AmazonS3 = 2,
    GoogleCloudStorage = 3
}

public enum StorageStatus
{
    Pending = 1,
    Uploaded =2,
    Failed = 3,
    Canceled =4,
    Deleted =5,
    Corrupted=6
}

public class StoredFile
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public Guid Id { get;  }
    public Guid FileCategoryId { get;  }
    public StoredFileCategory FileCategory { get;  }
    public Guid UploadedByUserId { get; }
    public Guid? StatusChangedByUserId { get; private set; }
    public DateTimeOffset UploadedAt { get; }
    public DateTimeOffset? StatusUpdatedAt { get; private set;  }
    public string HashMd5 { get;  }
    public string FileName { get; }
    public MediaTypeHeaderValue ContentType { get; }
    public long Size { get; }
    public StorageProvider Provider { get; }
    public StorageStatus Status { get; private set; }
    public string StoragePath { get; private set;  }
    
    protected StoredFile() { }

    [JsonConstructor]
    public StoredFile(
        Guid id,
        Guid fileCategoryId,
        StoredFileCategory fileCategory,
        Guid uploadedByUserId ,
        DateTimeOffset uploadedAt,
        long size, 
        StorageProvider provider,
        StorageStatus status, 
        string hashMd5,
        string fileName,
        MediaTypeHeaderValue contentType,
        string storagePath,
        Guid? statusChangedByUserId = null,
        DateTimeOffset? statusUpdatedAt = null)
    {
        Id = id;
        FileCategory = fileCategory ?? throw new ArgumentNullException(nameof(fileCategory));
        FileCategoryId = fileCategoryId;
        UploadedByUserId = uploadedByUserId;
        StatusChangedByUserId = statusChangedByUserId;
        UploadedAt = uploadedAt;
        StatusUpdatedAt = statusUpdatedAt;
        HashMd5 = hashMd5 ?? throw new ArgumentNullException(nameof(hashMd5));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        Size = size;
        Provider = provider;
        Status = status;
        StoragePath = storagePath;
        ValidateFile();
    }

    private void ValidateFile()
    {
        if(!FileCategory.AllowedContentTypes.Contains(ContentType))
            throw new InvalidOperationException($"Content type {ContentType} is not allowed for this file category.");
        
        if(FileCategory.MaxSizeInBytes.HasValue && Size > FileCategory.MaxSizeInBytes.Value)
            throw new InvalidOperationException($"File size {Size} exceeds the maximum allowed size " +
                                                $"of {FileCategory.MaxSizeInBytes.Value} bytes for this file category.");
        
        
    }

    public static StoredFile CreateUpload(
        
        StoredFileCategory fileCategory,
        UserTenantDto userTenantDto,
        string hashMd5,
        string fileName,
        long size,
        StorageProvider provider,
        Guid? fileId = null)
    {
        var id = fileId?? Guid.NewGuid();
        var tenantId = fileCategory.TenantId;
        var storagePath = CreateStoragePath(tenantId, id, fileName);
        var contentType = GetMediaType(fileName);
        
        fileCategory.ValidateAccess(userTenantDto);
        
        return new StoredFile(
            id: id,
            fileCategoryId: fileCategory.Id,
            fileCategory: fileCategory,
            uploadedByUserId: userTenantDto.Id,
            uploadedAt: DateTimeOffset.UtcNow,
            size: size,
            provider: provider,
            status: StorageStatus.Pending,
            hashMd5: hashMd5,
            fileName: fileName,
            contentType: contentType,
            storagePath: storagePath
        );
    }
    
    private static string CreateStoragePath(
        Guid tenantId, 
        Guid id, 
        string fileName)
    {
        var extension = GetExtension(fileName);
        return $"tenants/{tenantId}/files/{id}/{id}{extension}";
    }
    
    private static string GetExtension(string fileName)
    {
       return Path.GetExtension(fileName);
    }
    
    public static MediaTypeHeaderValue GetMediaType(string fileName)
    {
        if (ContentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            return MediaTypeHeaderValue.Parse(contentType);
        }
        
        return MediaTypeHeaderValue.Parse("application/octet-stream");
    }
    
   
    
    public void UpdateUploadedStatus()
    {
        
        Status = StorageStatus.Uploaded;
        StatusUpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void UpdateFailedUploadStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var minimumTime = TimeSpan.FromMinutes(5);

        if (now - StatusUpdatedAt < minimumTime)
            return;

        Status = StorageStatus.Failed;
        StatusUpdatedAt = now;
    }
    
    public void UpdateFailedCanceledStatus()
    {
        var now = DateTimeOffset.UtcNow;
        Status = StorageStatus.Canceled;
        StatusUpdatedAt = now;
    }
    
    public void UpdateStatus(Guid userId, StorageStatus status)
    {

        if (Status == status)
        {
            throw new InvalidOperationException($"Status is already {status.ToString()}.");
        }

        if (status == StorageStatus.Uploaded)
        {
            throw new InvalidOperationException($"Cannot update status to {StorageStatus.Uploaded.ToString()}.");
        }
        
        if(status == StorageStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot update status to {StorageStatus.Failed.ToString()}.");
        }
        
        Status = status;
        StatusChangedByUserId = userId;
        StatusUpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void UpdateCorruptedStatus()
    {
        if (Status == StorageStatus.Corrupted)
        {
            throw new InvalidOperationException($"Status is already {StorageStatus.Corrupted.ToString()}.");
        }

        Status = StorageStatus.Corrupted;
        StatusUpdatedAt = DateTimeOffset.UtcNow;
    }
    
    
}

