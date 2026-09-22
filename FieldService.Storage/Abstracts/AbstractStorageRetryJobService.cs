using System.Collections.Concurrent;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;



internal abstract class AbstractStorageRetryJobService : IQueueConsumer
{
    protected readonly IStorageFallbackService _storageFallbackService;
    protected readonly IStoredFileRepository _storedFileRepository;
    protected readonly IStoredFileService _storedFileService;
    protected readonly IStorageProviderFactory _storageProviderFactory;
    protected readonly ILogger<AbstractStorageRetryJobService> _logger;
    
    protected AbstractStorageRetryJobService(
        IStorageFallbackService storageFallbackService,
        IStoredFileRepository storedFileRepository,
        IStoredFileService storedFileService,
        IStorageProviderFactory storageProviderFactory,
        ILogger<AbstractStorageRetryJobService> logger
        )
    {
        _storageFallbackService = storageFallbackService ?? throw new ArgumentNullException(nameof(storageFallbackService));
        _storedFileRepository = storedFileRepository ?? throw new ArgumentNullException(nameof(storedFileRepository));
        _storedFileService = storedFileService ?? throw new ArgumentNullException(nameof(storedFileService));
        _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public abstract Task ExecuteAsync(Job job, CancellationToken ct = default);
    
    
    protected async Task<IEnumerable<StoredFile>> HasUploadsAsync(
        IEnumerable<StoredFile> files,
        CancellationToken ct = default)
    {
        if (!files.Any())
            return Enumerable.Empty<StoredFile>();
        
        var fileProviders = files
            .GroupBy(f => f.Provider)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        var hasUploaded = new ConcurrentBag<(StoredFile File, bool Exists)>();
        
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(
            fileProviders, 
            parallelOptions, 
            async (providerGroup, 
                cancellationToken) =>
        {
            try
            {
                var provider = _storageProviderFactory.GetProvider(providerGroup.Key);
                var existsResults = await provider.HasFilesAsync(providerGroup.Value, cancellationToken);
                foreach (var result in existsResults)
                    hasUploaded.Add(result);
            }
            
            catch (Exception ex)
            {
                _logger.LogProviderHasUploadedError(
                    LogLevel.Error,
                    providerGroup.Key,
                    providerGroup.Value.Select(f => f.Id).ToList(),
                    ex);
            }
           
        });
        return files.Where(f => hasUploaded.Any(h => h.File.Id == f.Id && h.Exists)).ToList();
    }
    
    protected async Task<IEnumerable<Guid>> DeleteFilesFromStorageAsync(
        IEnumerable<StoredFile> files, 
        CancellationToken ct)
    {
        var filesList = files as IReadOnlyCollection<StoredFile> ?? files.ToList();
    
        if (filesList.Count == 0)
            return Enumerable.Empty<Guid>();
        
        var deletedFileIds = new ConcurrentBag<Guid>();
        
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(filesList, parallelOptions, async (uploadedFile, cancellationToken) =>
        {
            
            var provider = _storageProviderFactory.GetProvider(uploadedFile.Provider);

            try
            {
                await provider.DeleteAsync(uploadedFile, cancellationToken);

                deletedFileIds.Add(uploadedFile.Id);
            }
            catch (Exception ex)
            {
                _logger.LogProviderDeleteError(
                    LogLevel.Error,
                    uploadedFile.Provider.ToString(),
                    uploadedFile.Id,
                    ex);
            }
            
            
        });

        return deletedFileIds;
    }
    
    

    
}