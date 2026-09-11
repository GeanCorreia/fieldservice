using FieldService.Shared.Dtos;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

public interface IStoredFileService
{
 Task<StoredFile?> GetByIdAsync(
        Guid id, 
        UserAuthentication user, 
        CancellationToken ct = default);

    Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<(Guid id, UserAuthentication user)> ids,
        bool partialResults = false,
        CancellationToken ct = default);

    Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        UserAuthentication user, 
        CancellationToken ct = default);
    
    Task SaveStoredFileAsync(
        StoredFile storedFile, 
        UserAuthentication user, 
        CancellationToken ct = default);
    
    Task SaveStoredFilesAsync(
        IEnumerable<(StoredFile storedFile, UserAuthentication user)> files,
        bool partialResults = false,
        CancellationToken ct = default);
    
    Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);

    Task UpdateFailedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        UserAuthentication user, 
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
        IEnumerable<Guid> ids, 
        UserAuthentication user,
        bool partialResults = false,
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        UserAuthentication user, 
        CancellationToken ct = default);
    
    Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        UserAuthentication user,
        CancellationToken ct = default);
}