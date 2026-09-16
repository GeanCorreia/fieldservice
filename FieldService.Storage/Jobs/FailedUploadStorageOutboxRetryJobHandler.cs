using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;

internal sealed record FailedUploadStorageOutboxRetryJob : Job
{
    public static readonly JobType JobType = "failed-upload-storage-outbox-retry";
    
    
    internal FailedUploadStorageOutboxRetryJob()
        : base(new JobContext(JobType))
    {
    }
    
}

internal sealed class FailedUploadOutboxRetryJobProducer : AbstractScheduleRecurringProducer<FailedUploadStorageOutboxRetryJobHandler>
{
    public FailedUploadOutboxRetryJobProducer(
        IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new FailedUploadStorageOutboxRetryJob();
    protected override string CronExpression => "43 * * * *";
}
internal class FailedUploadStorageOutboxRetryJobHandler : AbstractStorageRetryJobService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FailedUploadStorageOutboxRetryJobHandler(
        IStoredFileRepository storedFileRepository,
        IStoredFileService storedFileService,
        IStorageProviderFactory storageProviderFactory,
        ILogger<FailedUploadStorageOutboxRetryJobHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(storedFileRepository, storedFileService, storageProviderFactory,  logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Context.Type != FailedUploadStorageOutboxRetryJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
        
        await RetryFailedUploadsAsync(ct);
    }
    
    private async Task RetryFailedUploadsAsync(CancellationToken ct)
    {
        var failedUploadFallbackAsync = await _storedFileService
            .GetFailedUploadFallbackAsync(ct);
        
        if (!failedUploadFallbackAsync.Any())
        {
            return;
        }
        
        var failedIds = failedUploadFallbackAsync
            .Select(f => f.Id)
            .ToList();
        
        var persistentFiles = await _storedFileRepository
            .GetByIdsAsync(failedIds, ct);
        
        var persistentFileList = persistentFiles.ToList();
        
        var filesToRetry = persistentFileList
            .Where(f => f.Status == StorageStatus.Pending)
            .ToList();

        var uploadedFiles = await HasUploadsAsync(filesToRetry, ct);
        
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(uploadedFiles, parallelOptions, async (uploadedFile, cancellationToken) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateUploadedStatusAsync(uploadedFile.Id, cancellationToken);
                
            }
            catch (Exception ex)
            {
                _logger.LogSuccessfulUploadOutboxServiceError(
                    LogLevel.Error,
                    uploadedFile.Id,    
                    ex);
            }
        });
        
        var filesToUpdateFailedStatus = filesToRetry
            .Where(f => !uploadedFiles.Any(u => u.Id == f.Id))
            .ToList();
        
        if (filesToUpdateFailedStatus.Any())
        {
            await Parallel.ForEachAsync(filesToUpdateFailedStatus, parallelOptions, async (failedFile, cancellationToken) =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                    await storedFileService.UpdateFailedUploadStatusAsync(failedFile.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogFailedUploadOutboxServiceError(
                        LogLevel.Error,
                        failedFile.Id,
                        ex);
                }
            });
        }
    }
    
}