using System.Net.Http.Headers;
using FieldService.Cache.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Storage.Data;
using FieldService.Storage.Entities;
using FieldService.Storage.Exceptions;
using FieldService.Storage.Interfaces;
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
        UserAuthentication user, 
        CancellationToken ct = default)
    {
        var cachedFile = await _storedFileCacheService.GetByIdAsync(id, ct);
        if (cachedFile is not null)
        {
            cachedFile.FileCategory.ValidateAccess(user);
            return cachedFile;
        }
          
        
        var file = await _storedFileRepository.GetByIdAsync(id, ct);
        if (file is null)
        {
            return null;
        }
        
        await CreateCache(file);
        file.FileCategory.ValidateAccess(user);
        
        return file;

    }

    private async Task CreateCache(StoredFile file)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFileAsync(file);
        }
        catch (Exception ex)
        {
           // Ignore cache errors to avoid affecting main flow
        }
    }
    
    private async Task CreateCache(IEnumerable<StoredFile> files)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFilesAsync(files);
        }
        catch (Exception ex)
        {
            // Ignore cache errors to avoid affecting main flow
        }
    }

    public async Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<(Guid id, UserAuthentication user)> ids,
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
        
        await CreateCache(allFiles);
        
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
        UserAuthentication user, 
        CancellationToken ct = default)
    {
        storedFile.FileCategory.ValidateAccess(user);
        await _storedFileRepository.SaveStoredFileAsync(storedFile, ct);
        await CreateCache(storedFile);
    }

    public async Task SaveStoredFilesAsync(
        IEnumerable<(StoredFile storedFile, UserAuthentication user)> files,
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
        await CreateCache(fileList);
    }

    public async Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        UserAuthentication user, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(user);
        
        category.ValidateAccess(user);
        
        var repositoryFiles = await _storedFileRepository.GetByCategoryAsync(category, ct);


        if (repositoryFiles.Any())
        {
            await CreateCache(repositoryFiles);
        }

        return repositoryFiles;
    }
    
    

    public async Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        UserAuthentication user, 
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
            await CreateCache(category);
        }
        return category;
    }
    
    private async Task CreateCache(StoredFileCategory category)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFileCategoryAsync(category);
        }
        catch (Exception ex)
        {
            // Ignore cache errors to avoid affecting main flow
        }
    }
    
   

    public async Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
    IEnumerable<Guid> ids, 
    UserAuthentication user,
    bool partialResults = false,
    CancellationToken ct = default)
{
    ArgumentNullException.ThrowIfNull(ids);
    ArgumentNullException.ThrowIfNull(user);

    var requestedIds = ids.ToHashSet();
    if (requestedIds.Count == 0)
        return Enumerable.Empty<StoredFileCategory>();


    var cachedCategories = (await _storedFileCacheService.GetCategoryByIdsAsync(requestedIds, ct))?.ToList() 
                          ?? new List<StoredFileCategory>();
    
    var cachedIds = cachedCategories.Select(c => c.Id).ToHashSet();
    var missingIds = requestedIds.Except(cachedIds).ToList();

    var repositoryCategories = await _storedFileRepository.GetCategoryByIdsAsync(missingIds, ct);
    var repositoriesId = repositoryCategories.Select(c => c.Id).ToHashSet();
    
    await CreateCache(repositoryCategories);
    
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
    
    var filesToValidate = new List<(StoredFileCategory, UserAuthentication)>();

    foreach (var id in notFoundIds)
    {
        var category = founds.FirstOrDefault(c => c.Id == id);
        if (category != null)
        {
            filesToValidate.Add((category, user));
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
    
    private async Task CreateCache(IEnumerable<StoredFileCategory> categories)
    {
        try
        {
            await _storedFileCacheService.SaveStoredFilesCategoriesAsync(categories);
        }
        catch (Exception ex)
        {
            // Ignore cache errors to avoid affecting main flow
        }
    }

    public async Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        UserAuthentication user, 
        CancellationToken ct = default)
    {
        var userTenantIds = user.TenantDetails
            .Where(x => x.IsActive)
            .Select(x => x.TenantId)
            .ToHashSet();

        if (!userTenantIds.Contains(tenantId))
        {
            throw new UnauthorizedAccessException($"User does not have access to tenant {tenantId}.");
        }
        
        var categories = await _storedFileRepository.GetByCategoryTenantIdAsync(tenantId, ct);
        await CreateCache(categories);
        return categories;
    }

    public async Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        UserAuthentication user,
        CancellationToken ct = default)
    {
        var userTenantIds = user.TenantDetails
            .Where(x => x.IsActive)
            .Select(x => x.TenantId)
            .ToHashSet();
        
        var categoryTenantId = storedFileCategory.TenantId;
        
        if (!userTenantIds.Contains(categoryTenantId))
        {
            throw new UnauthorizedAccessException($"User does not have access to tenant {categoryTenantId}.");
        }
        
        await _storedFileRepository.SaveStoredFileCategoryAsync(storedFileCategory, ct);
        
        await CreateCache(storedFileCategory);
    }

    public async Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        
        var file = await _storedFileRepository.GetByIdAsync(fileId, ct);
        if (file is null)
            throw new FileNotFoundException($"Stored file with ID {fileId} not found.");
        
        file.UpdateUploadedStatus();
       
        await _storedFileRepository.UpdateUploadedStatusAsync(fileId, ct);
        
        await CreateCache(file);
    }

    public async Task UpdateFailedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        
        var file = await _storedFileRepository.GetByIdAsync(fileId, ct);
        if (file is null)
            throw new FileNotFoundException($"Stored file with ID {fileId} not found.");
        
        file.UpdateFailedStatus();
       
        await _storedFileRepository.UpdateFailedStatusAsync(fileId, ct);
        
        await _storedFileCacheService.UpdateFailedStatusAsync(fileId, ct);
        
        
    }
    
    private async Task<IEnumerable<StoredFileCategory>> GetUnauthorizedFiles(
        IEnumerable<(StoredFileCategory category, UserAuthentication user)> categories)
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
        IEnumerable<(StoredFile file, UserAuthentication user)> files)
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




