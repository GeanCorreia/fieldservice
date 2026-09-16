using FieldService.Shared.Services;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using FieldService.Storage.Factories;
using Microsoft.Extensions.Configuration;
using FieldService.Data.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Abstracts;
using FieldService.Storage.Channels;
using FieldService.Storage.Data;
using FieldService.Storage.Exceptions;
using FieldService.Storage.Logs;
using FieldService.Storage.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Services;

public class StorageService : AbstractStorageService, IStorageService
{

    private readonly ILogger<StorageService> _logger;
    private readonly StoredFileFailedUploadOutboxChannel _failedUploadChannel;
    private readonly StoredFileCanceledUploadOutboxChannel _canceledUploadChannel;

    public StorageService(
        StorageProviderFactory storageProviderFactory,
        IConfiguration configuration,
        IStoredFileService storedFileService,
        ISqlUnitOfWork<StorageDbContext> unitOfWork,
        ILogger<StorageService> logger,
        StoredFileFailedUploadOutboxChannel failedUploadChannel,
        StoredFileCanceledUploadOutboxChannel canceledUploadOutboxChannel,
        IServiceProvider serviceProvider)
        : base(
            storageProviderFactory, 
            configuration, 
            storedFileService, 
            unitOfWork, 
            serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _failedUploadChannel = failedUploadChannel ?? throw new ArgumentNullException(nameof(failedUploadChannel));
        _canceledUploadChannel = canceledUploadOutboxChannel ?? throw new ArgumentNullException(nameof(canceledUploadOutboxChannel));
    }
    
    


    public async Task<StoredFileUploadResponse> UploadAsync(
        StoredFileUploadRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_unitOfWork.HasActiveTransaction)
        {
           throw new StorageTransactionRequiredException();

        }
        
        var file = await ProviderUploadAsync(request, ct);
        
        _unitOfWork.OnRolledBack(async token =>
        {
            await _canceledUploadChannel.EnqueueAsync(
                file.Id, 
                CancellationToken.None);
        });
        
        return CreateResponse(file);
    }

    private async Task<StoredFile> ProviderUploadAsync(
         StoredFileUploadRequest request,
         CancellationToken ct = default)
     {
         using var scope = _serviceProvider.CreateScope();
         var dbContext = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
         var repository = scope.ServiceProvider.GetRequiredService<IStoredFileRepository>();

         var file = CreateStoredFile(request);
         
         if (file.FileCategory is not null)
         {
             dbContext.Attach(file.FileCategory);
         }
         
         await repository.SaveStoredFileAsync(file, ct);

         try
         {
             var provider = GetStorageProvider(file.Provider);
             
             await provider.UploadStreamAsync(
                 file,
                 request.Content,
                 ct);

         }

         catch(Exception ex)
         {
             await _failedUploadChannel.EnqueueAsync(file.Id, CancellationToken.None);
             
             _logger.LogProviderUploadError(
                 LogLevel.Error,
                 file.Provider.ToString(),
                 file.Id.ToString(),
                 ex.Message,
                 ex);
             
             
             throw;
         }
         return file;
     }
    

    public async Task<IEnumerable<StoredFileUploadResponse>> UploadBatchAsync(
        IEnumerable<StoredFileUploadRequest> requests, 
        bool partialSuccess = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        using var semaphore = new SemaphoreSlim(_maxParallelism);
        
        var files = new List<(StoredFile, UserTenantDto)>();
        
        
        var createFileTasks = requests.Select(async request =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var file = CreateStoredFile(request);
                files.Add((file, request.UserTenantDto));
            }
            catch 
            {
                if (!partialSuccess)
                    throw;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(createFileTasks);
        
        if (!_unitOfWork.HasActiveTransaction)
        {
            await UploadWithLocalTransactionAsync(files, ct);
           
        }
        
        var storedFiles = files.Select(f => f.Item1).ToList();
        return CreateResponse(storedFiles);
        
    }

    public async Task<StoredFileDownloadResponse> DownloadAsync(
        StoredFileDownloadRequest request, 
        CancellationToken ct = default)
    {
        
        var file = await _storedFileService.GetByIdAsync(request.FileId, request.UserTenantDto, ct);

        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(request.FileId));
        }
        if(file.Status == StorageStatus.Corrupted || 
           file.Status == StorageStatus.Deleted ||
           file.Status == StorageStatus.Canceled ||
           file.Status == StorageStatus.Failed)
        {
            throw new FileNotFoundException("File not found.", nameof(request.FileId));
        }
           
        var provider = GetStorageProvider(file.Provider);
        
        var stream = await provider.OpenReadStreamAsync(file, ct);
        
        return CreateDownloadResponse(file, stream);
    }
    
    

    public async Task<StoredFileCompressedDownloadsResponse> CompressedDownloadBatchAsync(
        StoredFileDownloadsRequest request, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsCompressed == false)
        {
            throw new InvalidOperationException("Compressed download is required.");
        }
        
        using var semaphore = new SemaphoreSlim(_maxParallelism);
        
        var files = new List<StoredFileDownloadResponse>();
        
        var downloadTasks = request.FileIds.Select(async id =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var localRequest = new StoredFileDownloadRequest(id, request.UserTenantDto);
                var downloadResponse = await DownloadAsync(localRequest, ct);
                files.Add(downloadResponse);
            }
            catch (Exception ex)
            {
                if (!request.PartialSuccess)
                    throw;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(downloadTasks);

        var contentList = files
            .Select(f => (f.File, f.Content)).ToList();
            
        return await ZipArchiveUtility.CreateZipArchiveAsync(
            contentList,
            request.FileName,
            ct: ct);
        
       }

    public async Task<StoredFileDownloadsResponse> DownloadBatchAsync(
        StoredFileDownloadsRequest request, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsCompressed)
        {
            throw new InvalidOperationException("Compressed download is required.");
        }
        
        using var semaphore = new SemaphoreSlim(_maxParallelism);
        
        var files = new List<StoredFileDownloadResponse>();
        
        var downloadTasks = request.FileIds.Select(async id =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var localRequest = new StoredFileDownloadRequest(id, request.UserTenantDto);
                var downloadResponse = await DownloadAsync(localRequest, ct);
                files.Add(downloadResponse);
            }
            catch (Exception ex)
            {
                if (!request.PartialSuccess)
                    throw;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(downloadTasks);

        return new StoredFileDownloadsResponse(files);
    }

    
    public async Task DeleteAsync(
        Guid fileId, 
        UserTenantDto userTenantDto, 
        CancellationToken ct = default)
    {
       var file = await _storedFileService.GetByIdAsync(fileId, userTenantDto, ct);

        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(fileId));
        }

        file.UpdateStatus(
            userTenantDto.Id,
            StorageStatus.Deleted);
        
        await _storedFileService.SaveStoredFileAsync(file, userTenantDto, ct);

    }

    public async Task UpdateUploadedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
        await _storedFileService.UpdateUploadedStatusAsync(fileId, ct);
        
    }

    public async Task UpdateFailedStatusAsync(
        Guid fileId, 
        CancellationToken ct = default)
    {
                
        await _storedFileService.UpdateFailedUploadStatusAsync(fileId, ct);
    }

    public async Task UpdateStatusAsync(
        Guid fileId, 
        UserTenantDto userTenantDto, 
        StorageStatus status, 
        CancellationToken ct = default)
    {
        
        var file = await _storedFileService.GetByIdAsync(fileId,userTenantDto, ct);

        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(fileId));
        }
        file.UpdateStatus(
            userTenantDto.Id,
            status);
        
        await _storedFileService.SaveStoredFileAsync(file,userTenantDto, ct);
    }
    
    private StoredFile CreateStoredFile(
        StoredFileUploadRequest request)
    {

       
        var hashMd5 = HashService.CreateHashMd5(request.Content);

        var size = request.Content.CanSeek
            ? request.Content.Length
            : throw new InvalidOperationException("Content stream must be seekable to get size.");

        var file = StoredFile.CreateUpload(
            fileCategory: request.FileCategory,
            userTenantDto: request.UserTenantDto,
            hashMd5: hashMd5,
            fileName: request.FileName,
            size: size,
            provider: _defaultStorageProvider,
            fileId: request.FileId);

        return file;
    }
    
    private StoredFileUploadResponse CreateResponse(StoredFile file)
    {
        return new StoredFileUploadResponse(
            FileId: file.Id,
            SizeInBytes: file.Size,
            ContentType: file.ContentType,
            HashMd5: file.HashMd5);
    }
    
    private async Task UploadWithLocalTransactionAsync(
        IEnumerable<(StoredFile storedFile, UserTenantDto user)> files, 

        CancellationToken ct)
    {
        var filesList = files.ToList();
        if (filesList.Count < 1)
        {
            return;
        }
        await _unitOfWork.BeginAsync(ct);
        
        _unitOfWork.OnCommitted(async token =>
        {
            foreach (var (storedFile, user) in filesList)
            {
                await _failedUploadChannel.EnqueueAsync(storedFile.Id, token);
            }
        });

        try
        {
            await _storedFileService.SaveStoredFilesAsync(files, ct: ct);
            
            await _unitOfWork.CommitAsync(ct);
            
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
    
    private IEnumerable<StoredFileUploadResponse> CreateResponse(IEnumerable<StoredFile> files)
    {
        var filesList = files.ToList();
        if (filesList.Count < 1)
        {
            throw new ArgumentException("No files were uploaded.", nameof(files));
        }
        
        return filesList.Select(file => CreateResponse(file));
    }
    
    private StoredFileDownloadResponse CreateDownloadResponse(StoredFile file, Stream stream)
    {
        return new StoredFileDownloadResponse(
            file,
            stream);
    }
}