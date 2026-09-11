using FieldService.Shared.Dtos;
using FieldService.Storage.Dtos;
using FieldService.Storage.Types;

namespace FieldService.Storage.Interfaces;

public interface IStoragePresignedUrlService
{
    Task<PresignedFileUploadResponseDto> CreateUploadUrlAsync(
        PresignedFileUploadRequestDto request, 
        Guid categoryId,
        UserAuthentication user,
        Guid? fileId = null,
        CancellationToken ct = default);

    Task<PresignedBatchFileUploadResponseDto> CreateBatchUploadUrlsAsync(
        IEnumerable<PresignedBatchFileUploadRequest> request,
        UserAuthentication user,
        bool partialSuccess = false,
        CancellationToken ct = default);
    
    Task<PresignedFileDownloadResponseDto> CreateDownloadUrlAsync(
        PresignedFileDownloadRequestDto request, 
        UserAuthentication user,
        CancellationToken ct = default);

    Task<PresignedBatchFileDownloadResponseDto> CreateBatchDownloadUrlsAsync(
        PresignedBatchFileDownloadRequestDto request, 
        UserAuthentication user,
        CancellationToken ct = default);
}