using System.Net.Http.Headers;
using FieldService.Cache.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Data;
using FieldService.Storage.Entities;
using FieldService.Storage.Exceptions;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Microsoft.Extensions.Logging;
using Role = FieldService.Shared.Types.Role;


namespace FieldService.Storage.Services;

internal sealed class StoredFileService : IStoredFileService
{
    
    private readonly ILogger<StoredFileService> _logger;
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IStoredFileCacheService _storedFileCacheService;

    public StoredFileService(
        ILogger<StoredFileService> logger,
        IStoredFileRepository storedFileRepository,
        IStoredFileCacheService storedFileCacheService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storedFileRepository = storedFileRepository ?? throw new ArgumentNullException(nameof(storedFileRepository));
        _storedFileCacheService = storedFileCacheService ?? throw new ArgumentNullException(nameof(storedFileCacheService));
    }
    public async Task<StoredFile?> GetByIdAsync(
        Guid id, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        
        
        var file = await _storedFileRepository.GetByIdAsync(id, ct);
        file.FileCategory.ValidateAccess(userTenantDto);
        
        return file;

    }
    
    private async Task<StoredFile> GetByIdAsync(
        Guid id, 
        CancellationToken ct = default)
    {
        var cachedFile = await _storedFileCacheService.GetByIdAsync(id, ct);
        if (cachedFile is not null)
        {
            return cachedFile;
        }
        
        var file = await _storedFileRepository.GetByIdAsync(id, ct);
        if (file is null)
        {
            throw new FileNotFoundException($"Stored file with ID {id} not found.");
        }
        
        await CreateCache(file, ct);
        
        return file;
    }

    private async Task CreateCache(StoredFile file,CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFileAsync(file, ct);
            
        }
        catch (Exception ex)
        {
           // Ignore cache errors to avoid affecting main flow
        }
    }
    
    private async Task CreateCache(IEnumerable<StoredFile> files,CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFilesAsync(files, ct);
        }
        catch (Exception ex)
        {
            // Ignore cache errors to avoid affecting main flow
        }
    }
    
    private async Task CreateFallbackFailedUploadCache(
        Guid fileId, 
        CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.CreateFallbackFailedUploadCache(fileId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogFallbackUploadError(
                LogLevel.Error,
                fileId.ToString(),
                ex.Message,
                ex);
        }
    }
    
    private async Task CreateFallbackSuccessUploadCache(
        Guid fileId, 
        CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.CreateFallbackSuccessUploadCache(fileId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogFallbackUploadError(
                LogLevel.Error,
                fileId.ToString(),
                ex.Message,
                ex);
        }
    }
    
    private async Task CreateFallbackCorruptedUploadCache(
        Guid fileId, 
        CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.CreateFallbackCorruptedUploadCache(fileId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogCorruptedUploadOutboxServiceError(
                LogLevel.Error,
                fileId,
                ex);
        }
    }
    
    private async Task CreateFallbackCanceledUploadCache(
        Guid fileId, 
        CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.CreateFallbackCanceledUploadCache(fileId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogFallbackUploadError(
                LogLevel.Error,
                fileId.ToString(),
                ex.Message,
                ex);
        }
    }
    

    public async Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<(Guid id, UserTenantDto user)> ids,
        bool partialResults = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var idList = ids.ToList();
        var fileIds = idList.Select(x => x.id).ToHashSet();
        
        var cachedFiles = await _storedFileCacheService.GetByIdsAsync(fileIds, ct);
        var cachedFileIds = cachedFiles.Select(f => f.Id).ToHashSet();

        var missingFileIds = fileIds.Except(cachedFileIds).ToHashSet();
        var missingFiles = await _storedFileRepository.GetByIdsAsync(missingFileIds, ct);
        
        var allFiles = cachedFiles.Concat(missingFiles).ToList();
        
        await CreateCache(allFiles, ct);
        
        var foundFileIds = allFiles.Select(f => f.Id).ToHashSet();
        var notFoundIds = fileIds.Except(foundFileIds).ToList();

        if (!partialResults && notFoundIds.Count > 0)
        {
            throw new FileNotFoundException(
                $"Some files were not found: [{string.Join(", ", notFoundIds)}]",
                string.Join(",", notFoundIds));;
        }


        var filesMap = allFiles.ToDictionary(f => f.Id);
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
            allFiles.RemoveAll(f => unauthorizedSet.Contains(f.Id));
        }

        return allFiles;
    }

    public async Task SaveStoredFileAsync(
        StoredFile storedFile, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        storedFile.FileCategory.ValidateAccess(userTenantDto);
        await _storedFileRepository.SaveStoredFileAsync(storedFile, ct);
        await CreateCache(storedFile, ct);
    }

    public async Task SaveStoredFilesAsync(
        IEnumerable<(StoredFile storedFile, UserTenantDto user)> files,
        bool partialResults = false,
        CancellationToken ct = default)
    {
        
        var unauthorizedFiles = GetUnauthorizedFiles(files).ToList();

        if (!partialResults && unauthorizedFiles.Count > 0)
        {
            var unauthorizedCategories = unauthorizedFiles
                .Select(f => f.FileCategory.Code)
                .ToHashSet();
            
            throw new UnauthorizedAccessException(
                $"User is not authorized to create files: [{string.Join(", ", unauthorizedCategories)}]");
        }
        
        var unauthorizedSet = unauthorizedFiles.Select(f => f.Id).ToHashSet();
        var fileList = files
            .Select(f => f.storedFile)
            .ToList();
        fileList.RemoveAll(f => unauthorizedSet.Contains(f.Id));
        await _storedFileRepository.SaveStoredFilesAsync(fileList, ct);
        await CreateCache(fileList, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(userTenantDto);
        
        category.ValidateAccess(userTenantDto);
        
        var repositoryFiles = await _storedFileRepository.GetByCategoryAsync(category, ct);


        if (repositoryFiles.Any())
        {
            await CreateCache(repositoryFiles, ct);
        }

        return repositoryFiles;
    }


    

    public async Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        var cacheCategory = await _storedFileCacheService.GetCategoryByIdAsync(id, ct);
        if (cacheCategory != null)
        {
            return cacheCategory;
        }
        
        var category = await _storedFileRepository.GetCategoryByIdAsync(id, ct);
        if (category != null)
        {
            await CreateCache(category, ct);
        }
        return category;
    }
    
    private async Task CreateCache(StoredFileCategory category, CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFileCategoryAsync(category, ct);
        }
        catch (Exception ex)
        {
            // Ignore cache errors to avoid affecting main flow
        }
    }
    
   

    public async Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
    IEnumerable<Guid> ids, 
    UserTenantDto userTenantDto,
    bool partialResults = false,
    CancellationToken ct = default)
{
    ArgumentNullException.ThrowIfNull(ids);
    ArgumentNullException.ThrowIfNull(userTenantDto);

    var requestedIds = ids.ToHashSet();
    if (requestedIds.Count == 0)
        return Enumerable.Empty<StoredFileCategory>();


    var cachedCategories = (await _storedFileCacheService.GetCategoryByIdsAsync(requestedIds, ct))?.ToList() 
                          ?? new List<StoredFileCategory>();
    
    var cachedIds = cachedCategories.Select(c => c.Id).ToHashSet();
    var missingIds = requestedIds.Except(cachedIds).ToList();

    var repositoryCategories = await _storedFileRepository.GetCategoryByIdsAsync(missingIds, ct);
    var repositoriesId = repositoryCategories.Select(c => c.Id).ToHashSet();
    
    await CreateCache(repositoryCategories, ct);
    
    var founds = new List<StoredFileCategory>();
    founds.AddRange(cachedCategories);
    founds.AddRange(repositoryCategories);
    var foundIds = founds.Select(c => c.Id).ToHashSet();
    
    var notFoundIds = requestedIds.Except(foundIds).ToList();

    if (!partialResults && notFoundIds.Count > 0)
    {
        throw new FileNotFoundException(
            $"Some files were not found: [{string.Join(", ", notFoundIds)}]",
            string.Join(",", notFoundIds));;
    }
    
    var filesToValidate = new List<(StoredFileCategory, UserTenantDto)>();

    foreach (var id in notFoundIds)
    {
        var category = founds.FirstOrDefault(c => c.Id == id);
        if (category != null)
        {
            filesToValidate.Add((category, userTenantDto));
        }
    }


    var unauthorizedCategories = (await GetUnauthorizedFiles(filesToValidate)).ToList();

    if (!partialResults && unauthorizedCategories.Count > 0)
    {
        var unauthorizedIds = unauthorizedCategories.Select(c => c.Id);
        throw new UnauthorizedAccessException(
            $"User is not authorized to access categories: [{string.Join(", ", unauthorizedIds)}]");
    }
    
    if (unauthorizedCategories.Count > 0)
    {
        var unauthorizedSet = unauthorizedCategories.Select(c => c.Id).ToHashSet();
        founds.RemoveAll(c => unauthorizedSet.Contains(c.Id));
    }

    return founds;
}
    
    private async Task CreateCache(
        IEnumerable<StoredFileCategory> categories,
        CancellationToken ct = default)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFilesCategoriesAsync(categories, ct);
        }
        catch (Exception ex)
        {
            // Ignore cache errors to avoid affecting main flow
        }
    }

    public async Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
        var userTenantId = userTenantDto.TenantDto.TenantId;

        if (userTenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"User does not have access to tenant {tenantId}.");
        }
        
        var categories = await _storedFileRepository.GetByCategoryTenantIdAsync(tenantId, ct);
        await CreateCache(categories, ct);
        return categories;
    }

    public async Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        UserTenantDto userTenantDto,
        CancellationToken ct = default)
    {
        var userTenantId = userTenantDto.TenantDto.TenantId;
        
        var categoryTenantId = storedFileCategory.TenantId;
        
        if (userTenantId != categoryTenantId)
        {
            throw new UnauthorizedAccessException($"User does not have access to tenant {categoryTenantId}.");
        }
        
        await _storedFileRepository.SaveStoredFileCategoryAsync(storedFileCategory, ct);
        
        await CreateCache(storedFileCategory, ct);
    }
    
    public async Task UpdateCanceledUploadStatusAsync(IEnumerable<Guid> fileIds, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateCorruptedUploadStatusAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct);
            if (file is null)
                throw new FileNotFoundException($"Stored file with ID {fileId} not found.");

            file.UpdateCorruptedStatus();

            await _storedFileRepository.SaveStoredFileAsync(file, ct);

            await CreateCache(file, ct);
            await _storedFileCacheService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex)
        {
            await CreateFallbackCorruptedUploadCache(
                fileId,
                ct);
        }
        
    }

    public async Task<IEnumerable<StoredFile>> GetFailedUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storedFileCacheService.GetFailedUploadFallbackAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetCanceledUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storedFileCacheService.GetCanceledUploadFallbackAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetSuccessUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storedFileCacheService.GetSuccessUploadFallbackAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetCorruptedUploadFallbackAsync(CancellationToken ct = default)
    {
        return await _storedFileCacheService.GetCorruptedUploadFallbackAsync(ct);
    }

    public async Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct);
            if (file is null)
                throw new FileNotFoundException($"Stored file with ID {fileId} not found.");
        
            file.UpdateUploadedStatus();
       
            await _storedFileRepository.SaveStoredFileAsync(file, ct);
        
            await CreateCache(file, ct);
            await _storedFileCacheService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex)
        {
              await CreateFallbackSuccessUploadCache(
                fileId,
                ct);
        }
    }

    public async Task UpdateUploadedStatusAsync(IEnumerable<Guid> fileIds, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateFailedUploadStatusAsync(IEnumerable<Guid> fileIds, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateCanceledUploadStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct);
            if (file is null)
                throw new FileNotFoundException($"Stored file with ID {fileId} not found.");

            file.UpdateFailedCanceledStatus();

            await _storedFileRepository.SaveStoredFileAsync(file, ct);

            await CreateCache(file, ct);
            await _storedFileCacheService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex)
        {
            await CreateFallbackCanceledUploadCache(
                fileId,
                ct);
        }
        
    }

    public async Task UpdateFailedUploadStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId, ct);
            if (file is null)
                throw new FileNotFoundException($"Stored file with ID {fileId} not found.");
        
            file.UpdateFailedUploadStatus();
       
            await _storedFileRepository.SaveStoredFileAsync(file, ct);
        
            await CreateCache(file, ct);
            await _storedFileCacheService.RemoveFallbackCached(file.Id, ct);
        }
        catch (Exception ex)
        {
           await CreateFallbackFailedUploadCache(
               fileId,
               ct);
        }
        
        
        
    }
    
    private async Task<IEnumerable<StoredFileCategory>> GetUnauthorizedFiles(
        IEnumerable<(StoredFileCategory category, UserTenantDto user)> categories)
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
    private IEnumerable<StoredFile> GetUnauthorizedFiles(
        IEnumerable<(StoredFile file, UserTenantDto user)> files)
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

    
}




