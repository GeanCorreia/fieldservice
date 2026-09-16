using System.Net.Http.Headers;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Types;

public record StoredFileUploadRequest(
    Stream Content,
    StoredFileCategory FileCategory,
    UserTenantDto UserTenantDto,
    string FileName,
    Guid? FileId = null) : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        Content?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Content is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Content != null)
        {
            Content.Dispose();
        }
    }
}

public record PresignedFileUploadRequest(
    UserTenantDto UserTenantDto,
    string FileName,
    long SizeInBytes,
    string HashMd5,
    Guid CategoryId,
    Guid? FileId = null);
    
public record StoredFileDownloadRequest(
    Guid FileId,
    UserTenantDto UserTenantDto);
    
public record StoredFileDownloadsRequest(
    IEnumerable<Guid> FileIds,
    UserTenantDto UserTenantDto,
    string? FileName = null,
    bool PartialSuccess = false,
    bool IsCompressed = true);

public record StoredFileDeleteRequest(
    Guid FileId,
    Guid UserId);

    