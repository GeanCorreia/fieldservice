using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Types;

namespace FieldService.Storage.Interfaces;

public interface IStorageService
{

    Task<StoredFileUploadResponse> UploadAsync(
        StoredFileUploadRequest request, 
        CancellationToken ct = default);
    
    Task<IEnumerable<StoredFileUploadResponse>> UploadBatchAsync(
        IEnumerable<StoredFileUploadRequest> request, 
        bool partialSuccess = false,
        CancellationToken ct = default);
    
    Task<StoredFileDownloadResponse> DownloadAsync(
        StoredFileDownloadRequest request, 
        CancellationToken ct = default);

    Task<StoredFileCompressedDownloadsResponse> CompressedDownloadBatchAsync(
        StoredFileDownloadsRequest request, 
        CancellationToken ct = default);
    
    Task<StoredFileDownloadsResponse> DownloadBatchAsync(
        StoredFileDownloadsRequest request, 
        CancellationToken ct = default);
    
    Task DeleteAsync(
        Guid fileId, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default);

    Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task UpdateFailedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default);
    
    Task UpdateStatusAsync(
        Guid fileId, 
        UserTenantDto userTenantDto,
        StorageStatus status, 
        CancellationToken ct = default);
}