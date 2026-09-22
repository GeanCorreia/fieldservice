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

    public StorageFallbackService(
        HybridCache hybridCache,
        IConfiguration configuration)
    {
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        _fallbackTtl = ResolveFallbackTtl(configuration);
    }

    public async Task CreateFallbackFailedUploadCache(Guid fileId, CancellationToken token = default)
    {
        ValidateFileId(fileId);
        var options = new HybridCacheEntryOptions { Expiration = _fallbackTtl };
        
        await _hybridCache.SetAsync(
            GetFallbackUploadFailedKey(fileId),
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
    
    

    private static void ValidateFileId(Guid fileId)
    {
        if (fileId == default)
            throw new ArgumentException("FileId is required.", nameof(fileId));
    }

    private static TimeSpan ResolveFallbackTtl(IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        return TimeSpan.FromHours(Math.Max(1, options.TtlFallBackHours));
    }

    private static string GetFileFallbackTag(Guid fileId) => $"fallback-tag:{fileId:N}";
    private static string GetFallbackUploadFailedKey(Guid fileId) => $"{FallbackFailedUploadPrefix}{fileId:N}";
    private static string GetFallbackCanceledUploadKey(Guid fileId) => $"{FallbackCanceledUploadPrefix}{fileId:N}";
    private static string GetFallbackSuccessUploadKey(Guid fileId) => $"{FallbackSuccessUploadPrefix}{fileId:N}";
    private static string GetFallbackCorruptedUploadKey(Guid fileId) => $"{FallbackCorruptedUploadPrefix}{fileId:N}";
    
}