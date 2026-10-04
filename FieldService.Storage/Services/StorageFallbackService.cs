using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;

namespace FieldService.Storage.Services;

internal sealed class StorageFallbackService : IStorageFallbackService
{
    private readonly HybridCache _hybridCache;
    private readonly TimeSpan _fallbackTtl;

    private const string StoragePrefix = "Storage";
    private const string FallbackFailedUploadPrefix = $"{StoragePrefix}:fallback-upload-failed:";
    private const string FallbackCanceledUploadPrefix = $"{StoragePrefix}:fallback-upload-canceled:";
    private const string FallbackSuccessUploadPrefix = $"{StoragePrefix}:fallback-upload-success:";
    private const string FallbackCorruptedUploadPrefix = $"{StoragePrefix}:fallback-upload-corrupted:";
    private const string FallbackFailedMarkDeletePrefix = $"{StoragePrefix}:fallback-mark-delete-failed:";
    private const string FallbackFailedDeletePrefix = $"{StoragePrefix}:fallback-delete-failed:";
    private const string FallbackFailedDeleteIndexKey = $"{StoragePrefix}:fallback-delete-failed:index";
    private const string FallbackFailedMarkDeleteIndexKey = $"{StoragePrefix}:fallback-mark-delete-failed:index";

    public StorageFallbackService(
        HybridCache hybridCache,
        IConfiguration configuration)
    {
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        _fallbackTtl = ResolveFallbackTtl(configuration);
    }

    public async Task CreateFallbackFailedDeleteCache(Guid fileId, Guid userId, CancellationToken token)
    {
        ValidateFileId(fileId);
        ValidateUserId(userId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };
        var payload = new StoredFileDeleteFallback(fileId, userId);
        await _hybridCache.SetAsync(
            GetFallbackFailedDeleteKey(fileId),
            payload,
            options,
            tags: [GetFileFallbackTag(fileId)],
            cancellationToken: token);

        await UpsertDeleteFallbackIndexAsync(FallbackFailedDeleteIndexKey, fileId, token);
    }

    public async Task CreateFallbackFailedMarkDeleteCache(Guid fileId, Guid userId, CancellationToken token)
    {
        ValidateFileId(fileId);
        ValidateUserId(userId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };
        var payload = new StoredFileDeleteFallback(fileId, userId);
        await _hybridCache.SetAsync(
            GetFallbackFailedMarkDeleteKey(fileId),
            payload,
            options,
            tags: [GetFileFallbackTag(fileId)],
            cancellationToken: token);

        await UpsertDeleteFallbackIndexAsync(FallbackFailedMarkDeleteIndexKey, fileId, token);
    }

    public async Task CreateFallbackFailedUploadCache(Guid fileId, CancellationToken token = default)
    {
        ValidateFileId(fileId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };
        
        await _hybridCache.SetAsync(
            GetFallbackFailedUploadKey(fileId),
            fileId,
            options,
            tags: [GetFileFallbackTag(fileId)],
            cancellationToken: token);
    }

    public async Task CreateFallbackCanceledUploadCache(Guid fileId, CancellationToken token = default)
    {
        ValidateFileId(fileId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };

        await _hybridCache.SetAsync(
            GetFallbackCanceledUploadKey(fileId),
            fileId,
            options,
            tags: [GetFileFallbackTag(fileId)],
            cancellationToken: token);
    }

    public async Task CreateFallbackSuccessUploadCache(Guid fileId, CancellationToken token = default)
    {
        ValidateFileId(fileId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };

        await _hybridCache.SetAsync(
            GetFallbackSuccessUploadKey(fileId),
            fileId,
            options,
            tags: [GetFileFallbackTag(fileId)],
            cancellationToken: token);
    }

    public async Task CreateFallbackCorruptedUploadCache(Guid fileId, CancellationToken token = default)
    {
        ValidateFileId(fileId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };

        await _hybridCache.SetAsync(
            GetFallbackCorruptedUploadKey(fileId),
            fileId,
            options,
            tags: [GetFileFallbackTag(fileId)],
            cancellationToken: token);
    }
    
    public async Task RemoveFallbackCached(Guid fileId, CancellationToken token = default)
    {
        ValidateFileId(fileId);
        await RemoveFromDeleteFallbackIndexesAsync(fileId, token);
        await _hybridCache.RemoveByTagAsync(GetFileFallbackTag(fileId), token);
    }
    

    public async Task<IEnumerable<StoredFile>> GetFailedUploadFallbackAsync(CancellationToken ct = default)
    {
        return await Task.FromResult(Enumerable.Empty<StoredFile>());
    }

    public async Task<IEnumerable<StoredFile>> GetCanceledUploadFallbackAsync(CancellationToken ct = default)
    {
        return await Task.FromResult(Enumerable.Empty<StoredFile>());
    }

    public async Task<IEnumerable<StoredFile>> GetSuccessUploadFallbackAsync(CancellationToken ct = default)
    {
        return await Task.FromResult(Enumerable.Empty<StoredFile>());
    }

    public async Task<IEnumerable<StoredFile>> GetCorruptedUploadFallbackAsync(CancellationToken ct = default)
    {
        return await Task.FromResult(Enumerable.Empty<StoredFile>());
    }

    public async Task<IEnumerable<StoredFileDeleteFallback>> GetFailedDeleteFallbackAsync(CancellationToken ct = default)
    {
        var ids = await ReadDeleteFallbackIndexAsync(FallbackFailedDeleteIndexKey, ct);
        return await ReadDeleteFallbackEntriesAsync(ids, GetFallbackFailedDeleteKey, ct);
    }

    public async Task<IEnumerable<StoredFileDeleteFallback>> GetFailedMarkDeleteFallbackAsync(CancellationToken ct = default)
    {
        var ids = await ReadDeleteFallbackIndexAsync(FallbackFailedMarkDeleteIndexKey, ct);
        return await ReadDeleteFallbackEntriesAsync(ids, GetFallbackFailedMarkDeleteKey, ct);
    }
    
    

    private static void ValidateFileId(Guid fileId)
    {
        if (fileId == default)
            throw new ArgumentException("FileId is required.", nameof(fileId));
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
    }

    private async Task UpsertDeleteFallbackIndexAsync(string indexKey, Guid fileId, CancellationToken ct)
    {
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };
        var ids = (await _hybridCache.GetOrCreateAsync(
            indexKey,
            _ => ValueTask.FromResult(new HashSet<Guid>()),
            cancellationToken: ct)) ?? new HashSet<Guid>();

        ids.Add(fileId);
        await _hybridCache.SetAsync(indexKey, ids, options, cancellationToken: ct);
    }

    private async Task RemoveFromDeleteFallbackIndexesAsync(Guid fileId, CancellationToken ct)
    {
        foreach (var indexKey in new[] { FallbackFailedDeleteIndexKey, FallbackFailedMarkDeleteIndexKey })
        {
            var ids = (await _hybridCache.GetOrCreateAsync(
                indexKey,
                _ => ValueTask.FromResult(new HashSet<Guid>()),
                cancellationToken: ct)) ?? new HashSet<Guid>();

            if (!ids.Remove(fileId))
            {
                continue;
            }

            await _hybridCache.SetAsync(
                indexKey,
                ids,
                new HybridCacheEntryOptions { Expiration = _fallbackTtl },
                cancellationToken: ct);
        }
    }

    private async Task<HashSet<Guid>> ReadDeleteFallbackIndexAsync(string indexKey, CancellationToken ct)
    {
        return (await _hybridCache.GetOrCreateAsync(
            indexKey,
            _ => ValueTask.FromResult(new HashSet<Guid>()),
            cancellationToken: ct)) ?? new HashSet<Guid>();
    }

    private async Task<IEnumerable<StoredFileDeleteFallback>> ReadDeleteFallbackEntriesAsync(
        IEnumerable<Guid> ids,
        Func<Guid, string> keyResolver,
        CancellationToken ct)
    {
        var entries = new List<StoredFileDeleteFallback>();
        foreach (var id in ids)
        {
            var key = keyResolver(id);
            var entry = await _hybridCache.GetOrCreateAsync<StoredFileDeleteFallback?>(
                key,
                _ => ValueTask.FromResult<StoredFileDeleteFallback?>(null),
                cancellationToken: ct);

            if (entry.HasValue)
            {
                entries.Add(entry.Value);
            }
        }

        return entries;
    }

    private static TimeSpan ResolveFallbackTtl(IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        return TimeSpan.FromHours(Math.Max(1, options.TtlFallBackHours));
    }

    private static string GetFileFallbackTag(Guid fileId) => $"fallback-tag:{fileId:N}";
    private static string GetFallbackFailedDeleteKey(Guid fileId) => $"{FallbackFailedDeletePrefix}{fileId:N}";
    private static string GetFallbackFailedMarkDeleteKey(Guid fileId) => $"{FallbackFailedMarkDeletePrefix}{fileId:N}";
    private static string GetFallbackFailedUploadKey(Guid fileId) => $"{FallbackFailedUploadPrefix}{fileId:N}";
    private static string GetFallbackCanceledUploadKey(Guid fileId) => $"{FallbackCanceledUploadPrefix}{fileId:N}";
    private static string GetFallbackSuccessUploadKey(Guid fileId) => $"{FallbackSuccessUploadPrefix}{fileId:N}";
    private static string GetFallbackCorruptedUploadKey(Guid fileId) => $"{FallbackCorruptedUploadPrefix}{fileId:N}";
    
}