using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;

namespace FieldService.Storage.Services;

internal sealed class StoredFileService : IStoredFileService
{
    private readonly ILogger<StoredFileService> _logger;
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IStorageFallbackService _storageFallbackService;
    private readonly HybridCache _hybridCache;

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24)
    };
    
    private static string FileKey(Guid id) => $"Storage:file:{id:N}";
    private static string CategoryKey(Guid id) => $"Storage:category:{id:N}";

    public StoredFileService(
        ILogger<StoredFileService> logger,
        IStoredFileRepository storedFileRepository,
        IStorageFallbackService storageFallbackService,
        HybridCache hybridCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storedFileRepository = storedFileRepository ?? throw new ArgumentNullException(nameof(storedFileRepository));
        _storageFallbackService = storageFallbackService ?? throw new ArgumentNullException(nameof(storageFallbackService));
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
    }
    

    public async Task<StoredFile?> GetByIdAsync(
        Guid id, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        if (id == default)
            throw new ArgumentException("FileId is required.", nameof(id));

        var file = await GetByIdInternalAsync(id, ct);
        if (file is null)
            return null;

        file.FileCategory.ValidateAccess(userTenantDto);
        return file;
    }

    public async Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<(Guid id, UserTenantDto user)> ids,
        bool partialResults = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var idList = ids.ToList();
        if (idList.Count == 0) return Enumerable.Empty<StoredFile>();

        var distinctFileIds = idList.Select(x => x.id).Where(x => x != default).Distinct().ToList();
        
        var cacheTasks = distinctFileIds.Select(async fileId =>
        {
            var file = await _hybridCache.GetOrCreateAsync<StoredFile?>(
                FileKey(fileId),
                _ => ValueTask.FromResult<StoredFile?>(null),
                CacheOptions,
                cancellationToken: ct);

            return (Id: fileId, File: file);
        });

        var cachedResults = await Task.WhenAll(cacheTasks);

        var foundFiles = new List<StoredFile>(distinctFileIds.Count);
        var missingIds = new List<Guid>();

        foreach (var item in cachedResults)
        {
            if (item.File is not null)
                foundFiles.Add(item.File);
            else
                missingIds.Add(item.Id);
        }
        
        if (missingIds.Count > 0)
        {
            var dbFiles = (await _storedFileRepository.GetByIdsAsync(missingIds, ct)).ToList();

            if (dbFiles.Count > 0)
            {
                var setCacheTasks = dbFiles.Select(file =>
                    _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct).AsTask());

                await Task.WhenAll(setCacheTasks);
                foundFiles.AddRange(dbFiles);
            }
        }
        
        var foundFileIds = foundFiles.Select(f => f.Id).ToHashSet();
        var notFoundIds = distinctFileIds.Except(foundFileIds).ToList();

        if (!partialResults && notFoundIds.Count > 0)
        {
            throw new FileNotFoundException(
                $"Some files were not found: [{string.Join(", ", notFoundIds)}]",
                string.Join(",", notFoundIds));
        }

        var filesMap = foundFiles.ToDictionary(f => f.Id);
        var filesToValidate = idList
            .Where(x => filesMap.ContainsKey(x.id))
            .Select(x => (File: filesMap[x.id], User: x.user));

        var unauthorizedFiles = GetUnauthorizedFiles(filesToValidate).ToList();

        if (!partialResults && unauthorizedFiles.Count > 0)
        {
            var unauthorizedIds = unauthorizedFiles.Select(f => f.Id);
            throw new UnauthorizedAccessException(
                $"User is not authorized to access files: [{string.Join(", ", unauthorizedIds)}]");
        }

        if (unauthorizedFiles.Count > 0)
        {
            var unauthorizedSet = unauthorizedFiles.Select(f => f.Id).ToHashSet();
            foundFiles.RemoveAll(f => unauthorizedSet.Contains(f.Id));
        }

        return foundFiles;
    }

    public async Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(userTenantDto);

        category.ValidateAccess(userTenantDto);

        var repositoryFiles = (await _storedFileRepository.GetByCategoryAsync(category, ct)).ToList();

        if (repositoryFiles.Count > 0)
        {
            var cacheTasks = repositoryFiles.Select(file =>
                _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct).AsTask());

            await Task.WhenAll(cacheTasks);
        }

        return repositoryFiles;
    }

    public async Task SaveStoredFileAsync(
        StoredFile storedFile, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFile);
        storedFile.FileCategory.ValidateAccess(userTenantDto);

        try
        {
            await _storedFileRepository.SaveStoredFileAsync(storedFile, ct);
        }
        catch (Exception ex)
        {
            _logger.LogStoredFileServiceCacheError(
                LogLevel.Error, 
                storedFile.Id, 
                ex);
        }

        await _hybridCache.SetAsync(FileKey(storedFile.Id), storedFile, CacheOptions, cancellationToken: ct);
    }

    public async Task SaveStoredFilesAsync(
        IEnumerable<(StoredFile storedFile, UserTenantDto user)> files,
        bool partialResults = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var fileListWithUsers = files.ToList();
        var unauthorizedFiles = GetUnauthorizedFiles(fileListWithUsers).ToList();

        if (!partialResults && unauthorizedFiles.Count > 0)
        {
            var unauthorizedCategories = unauthorizedFiles
                .Select(f => f.FileCategory.Code)
                .ToHashSet();

            throw new UnauthorizedAccessException(
                $"User is not authorized to create files: [{string.Join(", ", unauthorizedCategories)}]");
        }

        var unauthorizedSet = unauthorizedFiles.Select(f => f.Id).ToHashSet();
        var validFiles = fileListWithUsers
            .Select(f => f.storedFile)
            .Where(f => !unauthorizedSet.Contains(f.Id))
            .ToList();

        if (validFiles.Count == 0) return;

        await _storedFileRepository.SaveStoredFilesAsync(validFiles, ct);

        var cacheTasks = validFiles.Select(file =>
            _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct).AsTask());

        await Task.WhenAll(cacheTasks);
    }
    
    public async Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        if (id == default)
            throw new ArgumentException("CategoryId is required.", nameof(id));

        var category = await _hybridCache.GetOrCreateAsync(
            CategoryKey(id),
            async token => await _storedFileRepository.GetCategoryByIdAsync(id, token),
            CacheOptions,
            cancellationToken: ct);

        if (category is null)
            return null;

        category.ValidateAccess(userTenantDto);
        return category;
    }

    public async Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
        IEnumerable<Guid> ids, 
        UserTenantDto userTenantDto,
        bool partialResults = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(userTenantDto);

        var distinctCategoryIds = ids.Where(id => id != default).Distinct().ToList();
        if (distinctCategoryIds.Count == 0)
            return Enumerable.Empty<StoredFileCategory>();

        var cacheTasks = distinctCategoryIds.Select(async categoryId =>
        {
            var category = await _hybridCache.GetOrCreateAsync<StoredFileCategory?>(
                CategoryKey(categoryId),
                _ => ValueTask.FromResult<StoredFileCategory?>(null),
                CacheOptions,
                cancellationToken: ct);

            return (Id: categoryId, Category: category);
        });

        var cachedResults = await Task.WhenAll(cacheTasks);

        var foundCategories = new List<StoredFileCategory>(distinctCategoryIds.Count);
        var missingIds = new List<Guid>();

        foreach (var item in cachedResults)
        {
            if (item.Category is not null)
                foundCategories.Add(item.Category);
            else
                missingIds.Add(item.Id);
        }

        if (missingIds.Count > 0)
        {
            var dbCategories = (await _storedFileRepository.GetCategoryByIdsAsync(missingIds, ct)).ToList();

            if (dbCategories.Count > 0)
            {
                var setCacheTasks = dbCategories.Select(cat =>
                    _hybridCache.SetAsync(CategoryKey(cat.Id), cat, CacheOptions, cancellationToken: ct).AsTask());

                await Task.WhenAll(setCacheTasks);
                foundCategories.AddRange(dbCategories);
            }
        }

        var foundIds = foundCategories.Select(c => c.Id).ToHashSet();
        var notFoundIds = distinctCategoryIds.Except(foundIds).ToList();

        if (!partialResults && notFoundIds.Count > 0)
        {
            throw new FileNotFoundException(
                $"Some categories were not found: [{string.Join(", ", notFoundIds)}]",
                string.Join(",", notFoundIds));
        }

        var categoriesToValidate = foundCategories.Select(c => (c, userTenantDto));
        var unauthorizedCategories = GetUnauthorizedCategories(categoriesToValidate).ToList();

        if (!partialResults && unauthorizedCategories.Count > 0)
        {
            var unauthorizedIds = unauthorizedCategories.Select(c => c.Id);
            throw new UnauthorizedAccessException(
                $"User is not authorized to access categories: [{string.Join(", ", unauthorizedIds)}]");
        }

        if (unauthorizedCategories.Count > 0)
        {
            var unauthorizedSet = unauthorizedCategories.Select(c => c.Id).ToHashSet();
            foundCategories.RemoveAll(c => unauthorizedSet.Contains(c.Id));
        }

        return foundCategories;
    }

    public async Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        if (userTenantDto.TenantDto.TenantId != tenantId)
            throw new UnauthorizedAccessException($"User does not have access to tenant {tenantId}.");

        var categories = (await _storedFileRepository.GetByCategoryTenantIdAsync(tenantId, ct)).ToList();

        if (categories.Count > 0)
        {
            var setCacheTasks = categories.Select(cat =>
                _hybridCache.SetAsync(CategoryKey(cat.Id), cat, CacheOptions, cancellationToken: ct).AsTask());

            await Task.WhenAll(setCacheTasks);
        }

        return categories;
    }

    public async Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        UserTenantDto userTenantDto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFileCategory);

        if (userTenantDto.TenantDto.TenantId != storedFileCategory.TenantId)
            throw new UnauthorizedAccessException($"User does not have access to tenant {storedFileCategory.TenantId}.");

        await _storedFileRepository.SaveStoredFileCategoryAsync(storedFileCategory, ct);

        try
        {
            await _hybridCache.SetAsync(CategoryKey(storedFileCategory.Id), storedFileCategory, CacheOptions,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogStoredFileCategoryServiceCacheError(
                LogLevel.Error, 
                storedFileCategory.Id, 
                ex);
        }
        
    }

    public async Task UpdateUploadedStatusAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct)
                ?? throw new FileNotFoundException($"Stored file with ID {fileId} not found.");

            file.UpdateUploadedStatus();

            await _storedFileRepository.SaveStoredFileAsync(file, ct);
            await _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct);
            await _storageFallbackService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogFallbackUploadError(LogLevel.Error, fileId.ToString(), ex.Message, ex);
            await _storageFallbackService.CreateFallbackSuccessUploadCache(fileId, ct);
        }
    }

    public async Task UpdateUploadedStatusAsync(
        IEnumerable<Guid> fileIds, 
        CancellationToken ct = default)
    {
        var tasks = fileIds.Select(fileId => UpdateUploadedStatusAsync(fileId, ct));
        await Task.WhenAll(tasks);
    }

    public async Task UpdateFailedUploadStatusAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct)
                ?? throw new FileNotFoundException($"Stored file with ID {fileId} not found.");

            file.UpdateFailedUploadStatus();

            await _storedFileRepository.SaveStoredFileAsync(file, ct);
            await _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct);
            await _storageFallbackService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogFallbackUploadError(LogLevel.Error, fileId.ToString(), ex.Message, ex);
            await _storageFallbackService.CreateFallbackFailedUploadCache(fileId, ct);
        }
    }

    public async Task UpdateFailedUploadStatusAsync(IEnumerable<Guid> fileIds, CancellationToken ct = default)
    {
        var tasks = fileIds.Select(fileId => UpdateFailedUploadStatusAsync(fileId, ct));
        await Task.WhenAll(tasks);
    }

    public async Task UpdateCanceledUploadStatusAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct)
                ?? throw new FileNotFoundException($"Stored file with ID {fileId} not found.");

            file.UpdateFailedCanceledStatus();

            await _storedFileRepository.SaveStoredFileAsync(file, ct);
            await _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct);
            await _storageFallbackService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogFallbackUploadError(LogLevel.Error, fileId.ToString(), ex.Message, ex);
            await _storageFallbackService.CreateFallbackCanceledUploadCache(fileId, ct);
        }
    }

    public async Task UpdateCanceledUploadStatusAsync(IEnumerable<Guid> fileIds, CancellationToken ct = default)
    {
        var tasks = fileIds.Select(fileId => UpdateCanceledUploadStatusAsync(fileId, ct));
        await Task.WhenAll(tasks);
    }

    public async Task UpdateCorruptedUploadStatusAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct)
                ?? throw new FileNotFoundException($"Stored file with ID {fileId} not found.");

            file.UpdateCorruptedStatus();

            await _storedFileRepository.SaveStoredFileAsync(file, ct);
            await _hybridCache.SetAsync(FileKey(file.Id), file, CacheOptions, cancellationToken: ct);
            await _storageFallbackService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogCorruptedUploadOutboxServiceError(LogLevel.Error, fileId, ex);
            await _storageFallbackService.CreateFallbackCorruptedUploadCache(fileId, ct);
        }
    }

    public async Task<IEnumerable<StoredFile>> GetFailedUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storageFallbackService.GetFailedUploadFallbackAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetCanceledUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storageFallbackService.GetCanceledUploadFallbackAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetSuccessUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storageFallbackService.GetSuccessUploadFallbackAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetCorruptedUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storageFallbackService.GetCorruptedUploadFallbackAsync(ct);
    }

    private async Task<StoredFile?> GetByIdInternalAsync(Guid id, CancellationToken ct)
    {
        return await _hybridCache.GetOrCreateAsync(
            FileKey(id),
            async token => await _storedFileRepository.GetByIdAsync(id, token),
            CacheOptions,
            cancellationToken: ct);
    }

    private static IEnumerable<StoredFile> GetUnauthorizedFiles(IEnumerable<(StoredFile file, UserTenantDto user)> files)
    {
        var unauthorizedFiles = new List<StoredFile>();
        foreach (var (file, user) in files)
        {
            try
            {
                file.FileCategory.ValidateAccess(user);
            }
            catch (UnauthorizedAccessException)
            {
                unauthorizedFiles.Add(file);
            }
        }
        return unauthorizedFiles;
    }

    private static IEnumerable<StoredFileCategory> GetUnauthorizedCategories(IEnumerable<(StoredFileCategory category, UserTenantDto user)> categories)
    {
        var unauthorizedCategories = new List<StoredFileCategory>();
        foreach (var (category, user) in categories)
        {
            try
            {
                category.ValidateAccess(user);
            }
            catch (UnauthorizedAccessException)
            {
                unauthorizedCategories.Add(category);
            }
        }
        return unauthorizedCategories;
    }


    
}