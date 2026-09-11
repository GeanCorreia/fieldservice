using FieldService.Shared.Dtos;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

internal interface IStoredFileRepository
{
 
    Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken ct = default);
    
    Task SaveStoredFileAsync(
        StoredFile storedFile, 
        CancellationToken ct = default);
    
    Task SaveStoredFilesAsync(
        IEnumerable<StoredFile> files, 
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        CancellationToken ct = default);
    
    Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        CancellationToken ct = default);    
    
    Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default);
    
    Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        CancellationToken ct = default);
    
    Task SaveStoredFilesCategoriesAsync(
        IEnumerable<StoredFileCategory> categories, 
        CancellationToken ct = default);
    
    Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task UpdateFailedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
}