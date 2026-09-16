using FieldService.Data.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Abstracts;
using FieldService.Storage.Channels;
using FieldService.Storage.Data;
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
    private readonly ILogger<StoragePresignedUrlService> _logger;
    public StoragePresignedUrlService(
        StorageProviderFactory storageProviderFactory, 
        IConfiguration configuration, 
        IStoredFileService storedFileService, 
        ISqlUnitOfWork<StorageDbContext> unitOfWork,
        StoredFileFailedUploadOutboxChannel brokerMessageChannel,
        IServiceProvider serviceProvider,
        ILogger<StoragePresignedUrlService> logger) : base(
        storageProviderFactory, 
        configuration, 
        storedFileService, 
        unitOfWork,
        serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
  
    public async Task<PresignedFileUploadResponseDto> CreateUploadUrlAsync(
        PresignedFileUploadRequestDto request, 
        UserTenantDto userTenantDto,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        
        var file = await CreateStoredFileAsync(
            request, 
            request.CategoryId, 
            userTenantDto,
            fileId, 
            ct);
        
        var provider = GetStorageProvider();
        
        var url = await provider.GeneratePresignedUploadUrlAsync(
            file,
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
        UserTenantDto userTenantDto,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        var fileCategory = await _storedFileService.GetCategoryByIdAsync(categoryId, userTenantDto, ct);
        if (fileCategory == null)
            throw new InvalidOperationException("File category does not exist.");

        var size = request.SizeInBytes;
        var hashMd5 = request.HashMd5;

        var file = StoredFile.CreateUpload(
            fileCategory: fileCategory,
            userTenantDto: userTenantDto,
            hashMd5: hashMd5,
            fileName: request.FileName,
            size: size,
            provider: _defaultStorageProvider,
            fileId: fileId);

        return file;
    }

    public async Task<PresignedBatchFileUploadResponseDto> CreateBatchUploadUrlsAsync(
        IEnumerable<PresignedFileUploadRequest> request,
        UserTenantDto userTenantDto,
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
                    file.HashMd5,
                    file.CategoryId
                );
                
                var uploadResponse = await CreateUploadUrlAsync(
                    dto, 
                    userTenantDto,
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
        UserTenantDto userTenantDto,
        CancellationToken ct = default)
    {
        
        var file = await _storedFileService.GetByIdAsync(request.Id, userTenantDto, ct);
        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(request.Id));
        }
        
        if(file.Status == StorageStatus.Corrupted || 
           file.Status == StorageStatus.Deleted ||
           file.Status == StorageStatus.Canceled ||
           file.Status == StorageStatus.Failed)
        {
            throw new FileNotFoundException("File not found.", nameof(file.Id));
        }

        var provider = GetStorageProvider(file.Provider);
        
        var url = await provider.GeneratePresignedDownloadUrlAsync(
            file,
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
        UserTenantDto userTenantDto,
        CancellationToken ct = default)
    {
        var downloadResponses = new List<PresignedFileDownloadResponseDto>();

        foreach (var fileId in request.FileIds)
        {
            try
            {
                var downloadResponse = await CreateDownloadUrlAsync(
                    new PresignedFileDownloadRequestDto(fileId), 
                    userTenantDto, 
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