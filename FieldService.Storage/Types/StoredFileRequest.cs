using System.Net.Http.Headers;
using FieldService.Shared.Dtos;

namespace FieldService.Storage.Types;

public record StoredFileUploadRequest(
    Stream Content,
    Guid FileCategoryId,
    UserAuthentication user,
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

public record PresignedBatchFileUploadRequest(
    UserAuthentication user,
    string FileName,
    long SizeInBytes,
    string HashMd5,
    Guid CategoryId,
    Guid? FileId = null);
    
public record StoredFileDownloadRequest(
    Guid FileId,
    UserAuthentication user);
    
public record StoredFileDownloadsRequest(
    IEnumerable<Guid> FileIds,
    UserAuthentication user,
    string? FileName = null,
    bool PartialSuccess = false,
    bool IsCompressed = true);

public record StoredFileDeleteRequest(
    Guid FileId,
    Guid UserId);

    