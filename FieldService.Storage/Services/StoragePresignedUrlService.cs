using FieldService.Data.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Storage.Abstracts;
using FieldService.Storage.Channels;
using FieldService.Storage.Dtos;
using FieldService.Storage.Entities;
using FieldService.Storage.Factories;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Services;

public class StoragePresignedUrlService : AbstractStorageService, IStoragePresignedUrlService
{
    
    public StoragePresignedUrlService(
        StorageProviderFactory storageProviderFactory, 
        IConfiguration configuration, 
        IStoredFileService storedFileService, 
        IUnitOfWork unitOfWork,
        StoredFileOutboxChannel brokerMessageChannel,
        ILogger<StorageService> logger) : base(
        storageProviderFactory, 
        configuration, 
        storedFileService, 
        unitOfWork, logger, brokerMessageChannel)
    {
    }
    
  
    public async Task<PresignedFileUploadResponseDto> CreateUploadUrlAsync(
        PresignedFileUploadRequestDto request, 
        Guid categoryId,
        UserAuthentication user,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        
        var file = await CreateStoredFileAsync(
            request, 
            categoryId, 
            user,
            fileId, 
            ct);
        
        var provider = GetStorageProvider();
        var contentType = StoredFile.GetMediaType(request.FileName).ToString();
        
        var url = await provider.GeneratePresignedUploadUrlAsync(
            file.StoragePath,
            contentType,
            file.HashMd5,
            ExpiryPreSignedUrl,
            ct);
        
        return new PresignedFileUploadResponseDto(
            file.Id,
            url,
            DateTimeOffset.UtcNow.Add(ExpiryPreSignedUrl)
            );
    }
    
    private async Task<StoredFile> CreateStoredFileAsync(
        PresignedFileUploadRequestDto request, 
        Guid categoryId,
        UserAuthentication user,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        var fileCategory = await _storedFileService.GetCategoryByIdAsync(categoryId, user, ct);
        if (fileCategory == null)
            throw new InvalidOperationException("File category does not exist.");

        var size = request.SizeInBytes;
        var hashMd5 = request.HashMd5;

        var file = StoredFile.CreateUpload(
            fileCategory: fileCategory,
            userId: user.Id,
            hashMd5: hashMd5,
            fileName: request.FileName,
            size: size,
            fileId: fileId);

        return file;
    }

    public async Task<PresignedBatchFileUploadResponseDto> CreateBatchUploadUrlsAsync(
        IEnumerable<PresignedBatchFileUploadRequest> request,
        UserAuthentication user,
        bool partialSuccess = false,
        CancellationToken ct = default)
    {
        var fileList = request.ToList();
        if (fileList.Count < 1)
        {
            throw new ArgumentException("No files were provided.", nameof(request));
        }
        
        var uploadResponses = new List<PresignedFileUploadResponseDto>();

        foreach (var file in fileList)
        {
            try
            {
                var dto = new PresignedFileUploadRequestDto(
                    file.FileName,
                    file.SizeInBytes,
                    file.HashMd5
                );
                
                var uploadResponse = await CreateUploadUrlAsync(
                    dto, 
                    file.CategoryId,
                    user,
                    file.FileId, 
                    ct);

                uploadResponses.Add(uploadResponse);
            }
            catch (Exception ex)
            {
                if(!partialSuccess)
                {
                    throw;
                }
                
            }
        }
        return new PresignedBatchFileUploadResponseDto(uploadResponses);
    }

    public async Task<PresignedFileDownloadResponseDto> CreateDownloadUrlAsync(
        PresignedFileDownloadRequestDto request, 
        UserAuthentication user,
        CancellationToken ct = default)
    {
        
        var file = await _storedFileService.GetByIdAsync(request.Id, user, ct);

        var provider = GetStorageProvider(file.Provider);
        
        var url = await provider.GeneratePresignedDownloadUrlAsync(
            file.StoragePath,
            ExpiryPreSignedUrl,
            ct);
        
        return new PresignedFileDownloadResponseDto(
            file.Id,
            file.FileName,
            file.Size,
            file.HashMd5,
            file.ContentType.ToString(),
            url,
            DateTimeOffset.UtcNow.Add(ExpiryPreSignedUrl)
            );
    }

    public async Task<PresignedBatchFileDownloadResponseDto> CreateBatchDownloadUrlsAsync(
        PresignedBatchFileDownloadRequestDto request,
        UserAuthentication user,
        CancellationToken ct = default)
    {
        var downloadResponses = new List<PresignedFileDownloadResponseDto>();

        foreach (var fileId in request.FileIds)
        {
            try
            {
                var downloadResponse = await CreateDownloadUrlAsync(
                    new PresignedFileDownloadRequestDto(fileId), 
                    user, 
                    ct);

                downloadResponses.Add(downloadResponse);
            }
            catch (Exception ex)
            {
                if(!request.PartialSuccess)
                {
                    throw;
                }
                
            }
        }
        return new PresignedBatchFileDownloadResponseDto(downloadResponses);
    }
}