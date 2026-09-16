using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Dtos;
using FieldService.Storage.Types;

namespace FieldService.Storage.Interfaces;

public interface IStoragePresignedUrlService
{
    Task<PresignedFileUploadResponseDto> CreateUploadUrlAsync(
        PresignedFileUploadRequestDto request, 
        UserTenantDto userTenantDto,
        Guid? fileId = null,
        CancellationToken ct = default);

    Task<PresignedBatchFileUploadResponseDto> CreateBatchUploadUrlsAsync(
        IEnumerable<PresignedFileUploadRequest> request,
        UserTenantDto userTenantDto,
        bool partialSuccess = false,
        CancellationToken ct = default);
    
    Task<PresignedFileDownloadResponseDto> CreateDownloadUrlAsync(
        PresignedFileDownloadRequestDto request, 
        UserTenantDto userTenantDto,
        CancellationToken ct = default);

    Task<PresignedBatchFileDownloadResponseDto> CreateBatchDownloadUrlsAsync(
        PresignedBatchFileDownloadRequestDto request, 
        UserTenantDto userTenantDto,
        CancellationToken ct = default);
}