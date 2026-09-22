using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Storage.Jobs;

internal record CleanUpStorageJob : Job
{
    public static readonly JobType JobType = "storage-clean-up-job";

    internal CleanUpStorageJob()
        : base(new JobContext(JobType))
    {
    }
    
}

internal sealed class CleanUpStorageJobProducer : AbstractScheduleRecurringProducer<CleanUpStorageJobConsumer>
{
    public CleanUpStorageJobProducer(
        IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new CleanUpStorageJob();
    protected override string CronExpression => "0 1 * * *"; 
}

internal sealed class CleanUpStorageJobConsumer : AbstractStorageRetryJobService
{
    private readonly ICleanUpStoredFileRepository _cleanUpStoredFileRepository;
    private readonly int  _expiryPreSignedUrlMinutes;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public CleanUpStorageJobConsumer(
        IStorageFallbackService storageFallbackService,
        IOptions<StorageOptions> storageOptions,
        ICleanUpStoredFileRepository cleanUpStoredFileRepository,
        IStoredFileRepository storedFileRepository,
        IStoredFileService storedFileService,
        IStorageProviderFactory storageProviderFactory,
        ILogger<CleanUpStorageJobConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(storageFallbackService, storedFileRepository, storedFileService, storageProviderFactory, logger)
    {
        ArgumentNullException.ThrowIfNull(storageOptions);

        _cleanUpStoredFileRepository = cleanUpStoredFileRepository 
                                       ?? throw new ArgumentNullException(nameof(cleanUpStoredFileRepository));
        _expiryPreSignedUrlMinutes = storageOptions.Value.ExpiryPreSignedUrlMinutes;
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    

    public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Context.Type != CleanUpStorageJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        await CleanUpStorageAsync(ct);
    }

    private async Task CleanUpStorageAsync(CancellationToken ct)
    {
        var uploadedBefore = DateTimeOffset.UtcNow 
                             - TimeSpan.FromMinutes(_expiryPreSignedUrlMinutes)
                             - TimeSpan.FromMinutes(5); 
        
        var filesToCleanUp = await _cleanUpStoredFileRepository
            .GetByStatusAsync(
                StorageStatus.Pending,
                uploadedBefore,
                ct);

        if (!filesToCleanUp.Any())
        {
            return;
        }

        var filesFallback = new List<StoredFile>();
            
         var corruptedFiles = (await _storageFallbackService
            .GetCorruptedUploadFallbackAsync(ct)).ToList();
         
         var successfulFiles = (await _storageFallbackService
            .GetSuccessUploadFallbackAsync(ct)).ToList();
         
         var cancelledFiles = (await _storageFallbackService
             .GetCanceledUploadFallbackAsync(ct)).ToList();
         
         var failedFiles = (await _storageFallbackService
             .GetFailedUploadFallbackAsync(ct)).ToList();
         
         
        
        filesFallback.AddRange(corruptedFiles);
        filesFallback.AddRange(successfulFiles);
        filesFallback.AddRange(cancelledFiles);
        filesFallback.AddRange(failedFiles);

        filesToCleanUp = filesToCleanUp.Except(filesFallback).ToList();

        var uploadedFiles = (await HasUploadsAsync(filesToCleanUp, ct)).ToList();
        
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(uploadedFiles, parallelOptions, async (uploadedFile, ct) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateUploadedStatusAsync(uploadedFile.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogSuccessfulUploadOutboxServiceError(
                    LogLevel.Error,
                    uploadedFile.Id,
                    ex);
            }
        });
        
        filesToCleanUp = filesToCleanUp.Except(uploadedFiles).ToList();
        
        await Parallel.ForEachAsync(filesToCleanUp, parallelOptions, async (fileToCleanUp, ct) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateCanceledUploadStatusAsync(fileToCleanUp.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogCanceledUploadOutboxServiceError(
                    LogLevel.Error,
                    fileToCleanUp.Id,
                    ex);
            }
        });
        
        
    }
}