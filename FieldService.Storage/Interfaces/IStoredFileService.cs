using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

public interface IStoredFileService
{
 Task<StoredFile?> GetByIdAsync(
        Guid id, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default);

    Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<(Guid id, UserTenantDto user)> ids,
        bool partialResults = false,
        CancellationToken ct = default);

    Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default);
    
    Task SaveStoredFileAsync(
        StoredFile storedFile, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default);
    
    Task SaveStoredFilesAsync(
        IEnumerable<(StoredFile storedFile, UserTenantDto user)> files,
        bool partialResults = false,
        CancellationToken ct = default);
    
    Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task UpdateUploadedStatusAsync(
        IEnumerable<Guid> fileIds, 
        CancellationToken ct = default);

    Task UpdateFailedUploadStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task UpdateFailedUploadStatusAsync(
        IEnumerable<Guid> fileIds,
        CancellationToken ct = default);
    
    Task UpdateCanceledUploadStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task UpdateCanceledUploadStatusAsync(
        IEnumerable<Guid> fileIds,
        CancellationToken ct = default);
    
    Task UpdateCorruptedUploadStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);

    
    Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
        IEnumerable<Guid> ids, 
        UserTenantDto userTenantDto,
        bool partialResults = false,
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        UserTenantDto userTenantDto, 
        CancellationToken ct = default);
    
    Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory, 
        UserTenantDto userTenantDto,
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFile>> GetFailedUploadFallbackAsync( 
        CancellationToken ct = default);
    Task<IEnumerable<StoredFile>> GetCanceledUploadFallbackAsync(
        CancellationToken ct = default);
    Task<IEnumerable<StoredFile>> GetSuccessUploadFallbackAsync(
        CancellationToken ct = default);
    Task<IEnumerable<StoredFile>> GetCorruptedUploadFallbackAsync(
        CancellationToken ct = default);
    
    
}