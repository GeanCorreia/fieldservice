using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using FieldService.Cache.Interfaces;
using FieldService.Shared.Types;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FieldService.Storage.Services;

internal sealed class RedisStoredFileService(
    IRedisContext redisContext,
    IConfiguration configuration) : IStoredFileCacheService
{
    private readonly TimeSpan _cacheTtl = ResolveCacheTtl(configuration);
    private readonly TimeSpan _fallbackTtl = ResolveFallbackTtl(configuration);

    private const string StoragePrefix = "Storage";
    private const string FilePrefix = $"{StoragePrefix}:file:";
    private const string FileCategoryPrefix = $"{StoragePrefix}:file-category:";
    private const string CategoryPrefix = $"{StoragePrefix}:category:";
    private const string CategoryTenantPrefix = $"{StoragePrefix}:category-tenant:";
    private const string CategoryIdentityPrefix = $"{StoragePrefix}:category-identity:";
    private const string FallbackFailedUploadPrefix = $"{StoragePrefix}:fallback-upload-failed:";
    private const string FallbackCanceledUploadPrefix = $"{StoragePrefix}:fallback-upload-canceled:";
    private const string FallbackSuccessUploadPrefix = $"{StoragePrefix}:fallback-upload-success:";
    private const string FallbackCorruptedUploadPrefix = $"{StoragePrefix}:fallback-upload-corrupted:";

    private const string UpsertCategoryLua = @"
        local entityKey = KEYS[1]
        local tenantIndexKey = KEYS[2]
        local identityKey = KEYS[3]
        local payload = ARGV[1]
        local categoryId = ARGV[2]
        redis.call('SET', entityKey, payload)
        redis.call('SET', identityKey, categoryId)
        redis.call('SADD', tenantIndexKey, categoryId)
        return 1
    ";

    private const string RemoveCategoryLua = @"
        local entityKey = KEYS[1]
        local tenantIndexKey = KEYS[2]
        local identityKey = KEYS[3]
        local categoryId = ARGV[1]
        redis.call('DEL', entityKey)
        redis.call('DEL', identityKey)
        redis.call('SREM', tenantIndexKey, categoryId)
        return 1
    ";

    private const string UpsertFileLua = @"
        local entityKey = KEYS[1]
        local categoryIndexKey = KEYS[2]
        local payload = ARGV[1]
        local fileId = ARGV[2]
        redis.call('SET', entityKey, payload)
        redis.call('SADD', categoryIndexKey, fileId)
        return 1
    ";

    private const string RemoveFileLua = @"
        local entityKey = KEYS[1]
        local categoryIndexKey = KEYS[2]
        local fileId = ARGV[1]
        redis.call('DEL', entityKey)
        redis.call('SREM', categoryIndexKey, fileId)
        return 1
    ";

    public async Task<StoredFile?> GetByIdAsync(
        Guid id, 
        CancellationToken ct = default)
    {
        ValidateFileId(id);
        ct.ThrowIfCancellationRequested();

        return await TryGetCachedFileAsync(id, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ct.ThrowIfCancellationRequested();

        
        var results = new List<StoredFile>();
        foreach (var id in ids)
        {
            var file = await TryGetCachedFileAsync(id, ct);
            if (file is not null)
                results.Add(file);
        }

        return results;
    }

    public async Task SaveStoredFileAsync(
        StoredFile storedFile, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFile);
        ct.ThrowIfCancellationRequested();

        await UpsertFileCacheAsync(storedFile, ct);
    }

    public async Task SaveStoredFilesAsync(
        IEnumerable<StoredFile> files, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        ct.ThrowIfCancellationRequested();

        foreach (var storedFile in files)
        {
            if (storedFile is null)
                continue;

            await UpsertFileCacheAsync(storedFile, ct);
        }
    }

    public async Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ct.ThrowIfCancellationRequested();

        var fileIds = await GetFileIdsByCategoryFromCacheAsync(category.Id, ct);
        if (fileIds.Length == 0)
            return Array.Empty<StoredFile>();

        var files = new List<StoredFile>(fileIds.Length);
        foreach (var fileId in fileIds)
        {
            var file = await TryGetCachedFileAsync(fileId, ct);
            if (file is not null)
                files.Add(file);
        }

        return files;
    }

    public async Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        CancellationToken ct = default)
    {
        ValidateCategoryId(id);
        ct.ThrowIfCancellationRequested();

        return await TryGetCachedCategoryAsync(id, ct);
    }

    public async Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ct.ThrowIfCancellationRequested();

        var distinctIds = ids.Where(id => id != default).Distinct().ToArray();
        if (distinctIds.Length == 0)
            return Array.Empty<StoredFileCategory>();

        var categories = new List<StoredFileCategory>(distinctIds.Length);
        foreach (var categoryId in distinctIds)
        {
            var category = await TryGetCachedCategoryAsync(categoryId, ct);
            if (category is not null)
                categories.Add(category);
        }

        return categories;
    }

    public async Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }


    public async Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFileCategory);
        ct.ThrowIfCancellationRequested();

        await UpsertCategoryCacheAsync(storedFileCategory, ct);
    }

    public async Task SaveStoredFilesCategoriesAsync(IEnumerable<StoredFileCategory> categories, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ct.ThrowIfCancellationRequested();

        foreach (var category in categories)
        {
            if (category is null)
                continue;

            await UpsertCategoryCacheAsync(category, ct);
        }
    }

    public async Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        ValidateFileId(fileId);
        ct.ThrowIfCancellationRequested();

        var file = await TryGetCachedFileAsync(fileId, ct);
        if (file is null)
            return;

        file.UpdateUploadedStatus();
        await UpsertFileCacheAsync(file, ct);
    }

    public async Task UpdateFailedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        ValidateFileId(fileId);
        ct.ThrowIfCancellationRequested();

        var file = await TryGetCachedFileAsync(fileId, ct);
        if (file is null)
            return;

        file.UpdateFailedUploadStatus();
        await UpsertFileCacheAsync(file, ct);
    }

    public async Task CreateFallbackFailedUploadCache(
        Guid fileId, 
        CancellationToken token)
    {
        ValidateFileId(fileId);
        token.ThrowIfCancellationRequested();

        await redisContext.Database.StringSetAsync(
            GetFallbackUploadFailedKey(fileId),
            fileId.ToString("N"),
            _fallbackTtl);
    }
    
    public async Task CreateFallbackCanceledUploadCache(
        Guid fileId, 
        CancellationToken token)
    {
        
        ValidateFileId(fileId);
        token.ThrowIfCancellationRequested();

        await redisContext.Database.StringSetAsync(
            GetFallbackCanceledUploadKey(fileId),
            fileId.ToString("N"),
            _fallbackTtl);
    }

    public async Task CreateFallbackSuccessUploadCache(
        Guid fileId, 
        CancellationToken token)
    {
        ValidateFileId(fileId);
        token.ThrowIfCancellationRequested();

        await redisContext.Database.StringSetAsync(
            GetFallbackSuccessUploadKey(fileId),
            fileId.ToString("N"),
            _fallbackTtl);
    }

    public async Task CreateFallbackCorruptedUploadCache(Guid fileId, CancellationToken token)
    {
        ValidateFileId(fileId);
        token.ThrowIfCancellationRequested();

        await redisContext.Database.StringSetAsync(
            GetFallbackCorruptedUploadKey(fileId),
            fileId.ToString("N"),
            _fallbackTtl);
    }

    public async Task RemoveFallbackCached(Guid fileId, CancellationToken token)
    {
        ValidateFileId(fileId);
        token.ThrowIfCancellationRequested();

        await Task.WhenAll(
            redisContext.Database.KeyDeleteAsync(GetFallbackUploadFailedKey(fileId)),
            redisContext.Database.KeyDeleteAsync(GetFallbackCanceledUploadKey(fileId)),
            redisContext.Database.KeyDeleteAsync(GetFallbackSuccessUploadKey(fileId)),
            redisContext.Database.KeyDeleteAsync(GetFallbackCorruptedUploadKey(fileId)));
    }

    public async Task<bool> HasFallbackCached(Guid fileId, CancellationToken token)
    {
        ValidateFileId(fileId);
        token.ThrowIfCancellationRequested();

        return await Task.FromResult(
            redisContext.Database.KeyExists(GetFallbackUploadFailedKey(fileId)) ||
            redisContext.Database.KeyExists(GetFallbackCanceledUploadKey(fileId)) ||
            redisContext.Database.KeyExists(GetFallbackSuccessUploadKey(fileId)) ||
            redisContext.Database.KeyExists(GetFallbackCorruptedUploadKey(fileId)));
    }

    public async Task<IEnumerable<StoredFile>> GetFailedUploadFallbackAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await GetFallbackFilesAsync(FallbackFailedUploadPrefix, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetCanceledUploadFallbackAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await GetFallbackFilesAsync(FallbackCanceledUploadPrefix, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetSuccessUploadFallbackAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await GetFallbackFilesAsync(FallbackSuccessUploadPrefix, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetCorruptedUploadFallbackAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await GetFallbackFilesAsync(FallbackCorruptedUploadPrefix, ct);
    }


    private async Task<StoredFile?> TryGetCachedFileAsync(
        Guid id, 
        CancellationToken ct)
    {
        var value = await redisContext.Database.StringGetAsync(GetFileKey(id));
        if (!value.HasValue)
            return null;

        try
        {
            var entry = JsonSerializer.Deserialize<StoredFileCacheEntry>(value!);
            return entry is null ? null : ToStoredFile(entry);
        }
        catch
        {
            await redisContext.Database.KeyDeleteAsync(GetFileKey(id));
            return null;
        }
    }

    private async Task<StoredFileCategory?> TryGetCachedCategoryAsync(
        Guid id, 
        CancellationToken ct)
    {
        var value = await redisContext.Database.StringGetAsync(GetCategoryKey(id));
        if (!value.HasValue)
            return null;

        try
        {
            var entry = JsonSerializer.Deserialize<StoredFileCategoryCacheEntry>(value!);
            return entry is null ? null : ToStoredFileCategory(entry);
        }
        catch
        {
            await redisContext.Database.KeyDeleteAsync(GetCategoryKey(id));
            return null;
        }
    }

    private async Task UpsertCategoryCacheAsync(
        StoredFileCategory category, 
        CancellationToken ct)
    {
        var entry = ToCacheEntry(category);
        var payload = JsonSerializer.Serialize(entry);
        await redisContext.Database.ScriptEvaluateAsync(
            UpsertCategoryLua,
            keys: [GetCategoryKey(category.Id), GetCategoryTenantKey(category.TenantId), GetCategoryIdentityKey(category.TenantId, category.Code, category.Version.ToString())],
            values: [payload, category.Id.ToString("N")]);

        await ApplyCategoryCacheTtlAsync(category.Id, category.TenantId, category.Code, category.Version.ToString());
    }

    private async Task UpsertFileCacheAsync(
        StoredFile storedFile, 
        CancellationToken ct)
    {
        var entry = ToCacheEntry(storedFile);
        var payload = JsonSerializer.Serialize(entry);
        await redisContext.Database.ScriptEvaluateAsync(
            UpsertFileLua,
            keys: [GetFileKey(storedFile.Id), GetFileCategoryKey(storedFile.FileCategoryId)],
            values: [payload, storedFile.Id.ToString("N")]);

        await ApplyFileCacheTtlAsync(storedFile.Id, storedFile.FileCategoryId);
    }

    private async Task ApplyCategoryCacheTtlAsync(Guid categoryId, Guid tenantId, string code, string version)
    {
        await Task.WhenAll(
            redisContext.Database.KeyExpireAsync(GetCategoryKey(categoryId), _cacheTtl),
            redisContext.Database.KeyExpireAsync(GetCategoryTenantKey(tenantId), _cacheTtl),
            redisContext.Database.KeyExpireAsync(GetCategoryIdentityKey(tenantId, code, version), _cacheTtl));
    }

    private async Task ApplyFileCacheTtlAsync(Guid fileId, Guid categoryId)
    {
        await Task.WhenAll(
            redisContext.Database.KeyExpireAsync(GetFileKey(fileId), _cacheTtl),
            redisContext.Database.KeyExpireAsync(GetFileCategoryKey(categoryId), _cacheTtl));
    }

    private static TimeSpan ResolveCacheTtl(IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        return TimeSpan.FromMinutes(Math.Max(1, options.TtlCacheMinutes));
    }

    private static TimeSpan ResolveFallbackTtl(IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        return TimeSpan.FromHours(Math.Max(1, options.TtlFallBackHours));
    }

    private async Task<Guid[]> GetCategoryIdsByTenantFromCacheAsync(
        Guid tenantId, 
        CancellationToken ct)
    {
        var members = await redisContext.Database.SetMembersAsync(GetCategoryTenantKey(tenantId));
        return members
            .Select(m => m.ToString())
            .Where(v => Guid.TryParse(v, out _))
            .Select(Guid.Parse)
            .Distinct()
            .ToArray();
    }

    private async Task<Guid[]> GetFileIdsByCategoryFromCacheAsync(
        Guid categoryId, 
        CancellationToken ct)
    {
        var members = await redisContext.Database.SetMembersAsync(GetFileCategoryKey(categoryId));
        return members
            .Select(m => m.ToString())
            .Where(v => Guid.TryParse(v, out _))
            .Select(Guid.Parse)
            .Distinct()
            .ToArray();
    }

    private async Task<IEnumerable<StoredFile>> GetFallbackFilesAsync(
        string keyPrefix,
        CancellationToken ct)
    {
        var fileIds = await GetFallbackFileIdsAsync(keyPrefix, ct);
        if (fileIds.Length == 0)
            return Array.Empty<StoredFile>();

        var files = new List<StoredFile>(fileIds.Length);
        foreach (var fileId in fileIds)
        {
            ct.ThrowIfCancellationRequested();

            var file = await TryGetCachedFileAsync(fileId, ct);
            if (file is not null)
                files.Add(file);
        }

        return files;
    }

    private async Task<Guid[]> GetFallbackFileIdsAsync(
        string keyPrefix,
        CancellationToken ct)
    {
        var server = redisContext.Connection.GetServer(redisContext.Connection.GetEndPoints().First());
        var fileIds = new HashSet<Guid>();

        await foreach (var key in server.KeysAsync(pattern: $"{keyPrefix}*"))
        {
            ct.ThrowIfCancellationRequested();

            var keyValue = key.ToString();
            var fileIdToken = keyValue.Replace(keyPrefix, string.Empty, StringComparison.Ordinal);
            if (!Guid.TryParse(fileIdToken, out var fileId))
                continue;

            fileIds.Add(fileId);
        }

        return fileIds.ToArray();
    }

    private static StoredFileCategory ToStoredFileCategory(StoredFileCategoryCacheEntry entry)
    {
        return new StoredFileCategory(
            entry.Id,
            entry.TenantId,
            entry.Code,
            entry.MaxSizeInBytes,
            entry.AllowedContentTypes.Select(MediaTypeHeaderValue.Parse),
            SchemaVersion.FromString(entry.Version),
            entry.MinimumRequiredRole,
            entry.AllowedPermissions.Select(Permission.Create));
    }

    private static StoredFile ToStoredFile(StoredFileCacheEntry entry)
    {
        var category = ToStoredFileCategory(entry.Category);
        var ctor = typeof(StoredFile).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(c => c.GetParameters().Length == 13)
            ?? throw new InvalidOperationException("StoredFile private constructor was not found.");

        return (StoredFile)ctor.Invoke(new object?[]
        {
            entry.Id,
            category,
            entry.UploadedByUserId,
            entry.UploadedAt,
            entry.Size,
            entry.Provider,
            entry.Status,
            entry.HashMd5,
            entry.FileName,
            MediaTypeHeaderValue.Parse(entry.ContentType),
            entry.StoragePath,
            entry.StatusChangedByUserId,
            entry.StatusUpdatedAt
        });
    }

    private static StoredFileCategoryCacheEntry ToCacheEntry(StoredFileCategory category)
    {
        return new StoredFileCategoryCacheEntry(
            category.Id,
            category.TenantId,
            category.Code,
            category.MaxSizeInBytes,
            category.AllowedContentTypes.Select(x => x.ToString()).ToArray(),
            category.Version.ToString(),
            category.MinimumRequiredRole,
            (category.AllowedPermissions ?? Array.Empty<Permission>()).Select(x => x.ToString()).ToArray());
    }

    private static StoredFileCacheEntry ToCacheEntry(StoredFile file)
    {
        return new StoredFileCacheEntry(
            file.Id,
            file.FileCategoryId,
            file.UploadedByUserId,
            file.StatusChangedByUserId,
            file.UploadedAt,
            file.StatusUpdatedAt,
            file.HashMd5,
            file.FileName,
            file.ContentType.ToString(),
            file.Size,
            file.Provider,
            file.Status,
            file.StoragePath,
            ToCacheEntry(file.FileCategory));
    }

    private static void ValidateFileId(Guid fileId)
    {
        if (fileId == default)
            throw new ArgumentException("FileId is required.", nameof(fileId));
    }

    private static void ValidateCategoryId(Guid categoryId)
    {
        if (categoryId == default)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
    }

    private static string GetFileKey(Guid id) => $"{FilePrefix}{id:N}";
    private static string GetFileCategoryKey(Guid categoryId) => $"{FileCategoryPrefix}{categoryId:N}";
    private static string GetCategoryKey(Guid id) => $"{CategoryPrefix}{id:N}";
    private static string GetCategoryTenantKey(Guid tenantId) => $"{CategoryTenantPrefix}{tenantId:N}";
    private static string GetCategoryIdentityKey(Guid tenantId, string code, string version) => $"{CategoryIdentityPrefix}{tenantId:N}:{code}:{version}";
    private static string GetFallbackUploadFailedKey(Guid fileId) => $"{FallbackFailedUploadPrefix}{fileId:N}";
    private static string GetFallbackCanceledUploadKey(Guid fileId) => $"{FallbackCanceledUploadPrefix}{fileId:N}";
    private static string GetFallbackSuccessUploadKey(Guid fileId) => $"{FallbackSuccessUploadPrefix}{fileId:N}";
    private static string GetFallbackCorruptedUploadKey(Guid fileId) => $"{FallbackCorruptedUploadPrefix}{fileId:N}";

    private sealed record StoredFileCategoryCacheEntry(
        Guid Id,
        Guid TenantId,
        string Code,
        long? MaxSizeInBytes,
        string[] AllowedContentTypes,
        string Version,
        FieldService.Shared.Types.Role? MinimumRequiredRole,
        string[] AllowedPermissions);

    private sealed record StoredFileCacheEntry(
        Guid Id,
        Guid FileCategoryId,
        Guid UploadedByUserId,
        Guid? StatusChangedByUserId,
        DateTimeOffset UploadedAt,
        DateTimeOffset? StatusUpdatedAt,
        string HashMd5,
        string FileName,
        string ContentType,
        long Size,
        StorageProvider Provider,
        StorageStatus Status,
        string StoragePath,
        StoredFileCategoryCacheEntry Category);
}
