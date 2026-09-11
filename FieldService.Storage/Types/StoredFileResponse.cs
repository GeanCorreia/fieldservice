using System.Net.Http.Headers;
using FieldService.Storage.Entities;


namespace FieldService.Storage.Types;

public record StoredFileUploadResponse(
    Guid FileId,
    long SizeInBytes,
    MediaTypeHeaderValue ContentType,
    string HashMd5);

public record StoredFileDownloadResponse(
    StoredFile File,
    Stream Content
);

public record StoredFileZipEntry(
    ZipEntryPath EntryPath, 
    StoredFile File        
);

public readonly record struct ZipEntryPath
{
    public string Value { get; }

    public ZipEntryPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        
        var normalized = value.TrimStart('/', '\\').Replace('\\', '/');

        if (normalized.Contains(".."))
            throw new ArgumentException("Directory traversal path ('..') is not allowed in zip entry paths.", nameof(value));

        Value = normalized;
    }
    public static implicit operator string(ZipEntryPath path) => path.Value;
    public static explicit operator ZipEntryPath(string path) => new(path);

    public override string ToString() => Value;
}

public record StoredFileCompressedDownloadsResponse(
    IReadOnlyList<StoredFileZipEntry> Entries,
    Stream Content,
    string ZipFileName
) : IAsyncDisposable, IDisposable
{
    public IReadOnlyDictionary<ZipEntryPath, StoredFile> ToDictionary() =>
        Entries.ToDictionary(e => e.EntryPath, e => e.File);

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

public record StoredFileDownloadsResponse(
    IEnumerable<StoredFileDownloadResponse> Files);

