using FieldService.Shared.Services;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using FieldService.Storage.Configuration;
using FieldService.Storage.Factories;
using Microsoft.Extensions.Configuration;
using FieldService.Data.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Storage.Abstracts;
using FieldService.Storage.Channels;
using FieldService.Storage.Utils;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Services;

public class StorageService : AbstractStorageService, IStorageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageProviderFactory _storageProviderFactory;
    private readonly int _expiryPreSignedUrlMinutes;
    private readonly StorageProvider _defaultStorageProvider;
    private readonly IStoredFileService _storedFileService;
    private readonly int _maxParallelism;
    private readonly ILogger<StorageService> _logger;
    private readonly StoredFileOutboxChannel _brokerMessageChannel;
    
    public StorageService(
        StorageProviderFactory storageProviderFactory,
        IConfiguration configuration,
        IStoredFileService storedFileService,
        IUnitOfWork unitOfWork,
        ILogger<StorageService> logger,
        StoredFileOutboxChannel brokerMessageChannel)
        : base(storageProviderFactory, configuration, storedFileService, unitOfWork, logger, brokerMessageChannel)
    {
        
    }
    
    


    public async Task<StoredFileUploadResponse> UploadAsync(
        StoredFileUploadRequest request,
        CancellationToken ct = default)
    {

        var file = await CreateStoredFileAsync(request, ct);
        
        if (!_unitOfWork.HasActiveTransaction)
        {
            await UploadWithLocalTransactionAsync(file, request.user, ct);
            return CreateResponse(file);
        }

        _unitOfWork.OnCommitted(async token =>
        {
            await _brokerMessageChannel.EnqueueAsync(file, token);
        });
        
        await _storedFileService.SaveStoredFileAsync(file, request.user,ct);

        return CreateResponse(file);
    }
    

    public async Task<IEnumerable<StoredFileUploadResponse>> UploadBatchAsync(
        IEnumerable<StoredFileUploadRequest> requests, 
        bool partialSuccess = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        using var semaphore = new SemaphoreSlim(_maxParallelism);
        
        var files = new List<(StoredFile, UserAuthentication)>();
        
        
        var createFileTasks = requests.Select(async request =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var file = await CreateStoredFileAsync(request, ct);
                files.Add((file, request.user));
            }
            catch (Exception ex)
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
        
        var file = await _storedFileService.GetByIdAsync(request.FileId, request.user, ct);

        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(request.FileId));
        }
           
        var provider = GetStorageProvider(file.Provider);

        var stream = await provider.OpenReadStreamAsync(file.StoragePath, ct);
        
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
                var localRequest = new StoredFileDownloadRequest(id, request.user);
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
                var localRequest = new StoredFileDownloadRequest(id, request.user);
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
        UserAuthentication user, 
        CancellationToken ct = default)
    {
       var file = await _storedFileService.GetByIdAsync(fileId, user, ct);

        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(fileId));
        }

        file.UpdateStatus(
            user.Id,
            StorageStatus.Deleted);
        
        await _storedFileService.SaveStoredFileAsync(file, user, ct);

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
                
        await _storedFileService.UpdateFailedStatusAsync(fileId, ct);
    }

    public async Task UpdateStatusAsync(
        Guid fileId, 
        UserAuthentication user, 
        StorageStatus status, 
        CancellationToken ct = default)
    {
        
        var file = await _storedFileService.GetByIdAsync(fileId,user, ct);

        if (file == null)
        {
            throw new FileNotFoundException("File not found.", nameof(fileId));
        }
        file.UpdateStatus(
            user.Id,
            status);
        
        await _storedFileService.SaveStoredFileAsync(file,user, ct);
    }
    
    private async Task<StoredFile?> CreateStoredFileAsync(
        StoredFileUploadRequest request, 
        CancellationToken ct)
    {
        var fileCategory = await _storedFileService.GetCategoryByIdAsync(request.FileCategoryId, request.user, ct);
        if (fileCategory == null)
            throw new InvalidOperationException("File category does not exist.");
        
        var hashMd5 = HashService.CreateHashMd5(request.Content);

        var size = request.Content.CanSeek
            ? request.Content.Length
            : throw new InvalidOperationException("Content stream must be seekable to get size.");

        var file = StoredFile.CreateUpload(
            fileCategory: fileCategory,
            userId: request.user.Id,
            hashMd5: hashMd5,
            fileName: request.FileName,
            size: size,
            fileId: request.FileId);

        return file;
    }
    
    
    
    private async Task UploadWithLocalTransactionAsync(
        StoredFile file,
        UserAuthentication user,
        CancellationToken ct)
    {
        await _unitOfWork.BeginAsync(ct);
        
        _unitOfWork.OnCommitted(async token =>
        {
            await _brokerMessageChannel.EnqueueAsync(file, token);
        });

        try
        {
            await _storedFileService.SaveStoredFileAsync(file, user, ct);
            await _unitOfWork.CommitAsync(ct);
            
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
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
        IEnumerable<(StoredFile storedFile, UserAuthentication user)> files, 

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
                await _brokerMessageChannel.EnqueueAsync(storedFile, token);
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