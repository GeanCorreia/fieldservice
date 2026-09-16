using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;



internal sealed record CorruptedUploadStorageOutboxRetryJob : Job
{
    public static readonly JobType JobType = "corrupted-upload-storage-outbox-retry";  
    
    
    internal CorruptedUploadStorageOutboxRetryJob()
        : base(new JobContext(JobType))
    {
    }
    
}

internal sealed class CorruptedUploadOutboxRetryJobProducer : AbstractScheduleRecurringProducer<CorruptedUploadStorageOutboxRetryJobHandler>
{
    public CorruptedUploadOutboxRetryJobProducer(
        IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new CorruptedUploadStorageOutboxRetryJob();
    protected override string CronExpression => "33 * * * *";
}

internal class CorruptedUploadStorageOutboxRetryJobHandler : AbstractStorageRetryJobService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CorruptedUploadStorageOutboxRetryJobHandler(
        IStoredFileRepository storedFileRepository,
        IStoredFileService storedFileService,
        IStorageProviderFactory storageProviderFactory,
        ILogger<CorruptedUploadStorageOutboxRetryJobHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(storedFileRepository, storedFileService, storageProviderFactory, logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Context.Type != CorruptedUploadStorageOutboxRetryJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        await RetryCorruptedUploadsAsync(ct);
    }
    
    private async Task RetryCorruptedUploadsAsync(CancellationToken ct)
    {
        var corruptedUploadFallbackAsync = await _storedFileService
            .GetCorruptedUploadFallbackAsync(ct);
        
        if (!corruptedUploadFallbackAsync.Any())
        {
            return;
        }
        
        var corruptedIds = corruptedUploadFallbackAsync
            .Select(f => f.Id)
            .ToList();
        
        var persistentFiles = await _storedFileRepository
            .GetByIdsAsync(corruptedIds, ct);
        
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
        
        var filesToUpdateCorruptedStatus  =pendingFiles
            .Where(f => deletedFileIds.Contains(f.Id) || uploadedFiles.Any(u => u.Id == f.Id))
            .ToList();
        
        await Parallel.ForEachAsync(filesToUpdateCorruptedStatus, parallelOptions, async (pendingFile, cancellationToken) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateCorruptedUploadStatusAsync(pendingFile.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogCorruptedUploadOutboxServiceError(
                    LogLevel.Error,
                    pendingFile.Id,
                    ex);
            }
        });
    }
}