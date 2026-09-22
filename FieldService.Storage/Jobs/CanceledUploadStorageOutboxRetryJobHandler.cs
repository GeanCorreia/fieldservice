using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;

internal sealed record CanceledUploadStorageOutboxRetryJob : Job
{
    public static readonly JobType JobType = "canceled-upload-storage-outbox-retry";
    
    
    internal CanceledUploadStorageOutboxRetryJob()
        : base(new JobContext(JobType))
    {
    }
    
}

internal sealed class CanceledUploadOutboxRetryJobProducer : AbstractScheduleRecurringProducer<CanceledUploadStorageOutboxRetryJobHandler>
{
    public CanceledUploadOutboxRetryJobProducer(
        IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new CanceledUploadStorageOutboxRetryJob();
    protected override string CronExpression => "3 * * * *";
}

internal class CanceledUploadStorageOutboxRetryJobHandler : AbstractStorageRetryJobService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CanceledUploadStorageOutboxRetryJobHandler(
        IStorageFallbackService storageFallbackService,
        IStoredFileRepository storedFileRepository,
        IStoredFileService storedFileService,
        IStorageProviderFactory storageProviderFactory,
        ILogger<CanceledUploadStorageOutboxRetryJobHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(storageFallbackService, storedFileRepository, storedFileService, storageProviderFactory, logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Context.Type != CanceledUploadStorageOutboxRetryJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        await RetryCanceledUploadsAsync(ct);
    }
    
    private async Task RetryCanceledUploadsAsync( CancellationToken ct)
    {
        var canceledUploadFallbackAsync = await _storageFallbackService
            .GetCanceledUploadFallbackAsync(ct);
        
        if (!canceledUploadFallbackAsync.Any())
        {
            return;
        }
        
        var canceledIds = canceledUploadFallbackAsync
            .Select(f => f.Id)
            .ToList();
        
        var persistentFiles = await _storedFileRepository
            .GetByIdsAsync(canceledIds, ct);
        
        var pendingFiles = persistentFiles
            .Where(f => f.Status == StorageStatus.Pending)
            .ToList();
        

        
        var uploadedFiles = await HasUploadsAsync(pendingFiles, ct);
        var deletedFileIds = (await DeleteFilesFromStorageAsync(uploadedFiles, ct))
            .ToList();
        
        
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct
        };
        
        var filesToUpdateCanceledStatus = pendingFiles
            .Where(f => deletedFileIds.Contains(f.Id) || uploadedFiles.Any(u => u.Id == f.Id))
            .ToList();
        
        await Parallel.ForEachAsync(filesToUpdateCanceledStatus, parallelOptions, async (pendingFile, cancellationToken) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateCanceledUploadStatusAsync(pendingFile.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogCanceledUploadOutboxServiceError(
                    LogLevel.Error,
                    pendingFile.Id,
                    ex);
            }
        });
    }
    
}