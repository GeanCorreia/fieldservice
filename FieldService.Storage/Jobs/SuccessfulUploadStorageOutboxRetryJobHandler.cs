using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;

internal sealed record SuccessfulUploadStorageOutboxRetryJob : Job
{
    public static readonly JobType JobType = "successful-upload-storage-outbox-retry";  
    
    
    internal SuccessfulUploadStorageOutboxRetryJob()
        : base(new JobContext(JobType))
    {
    }
    
}

internal sealed class SuccessfulUploadOutboxRetryJobProducer : AbstractScheduleRecurringProducer<SuccessfulUploadStorageOutboxRetryJobHandler>
{
    public SuccessfulUploadOutboxRetryJobProducer(
        IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new SuccessfulUploadStorageOutboxRetryJob();
    protected override string CronExpression => "23 * * * *";
}

internal class SuccessfulUploadStorageOutboxRetryJobHandler : AbstractStorageRetryJobService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SuccessfulUploadStorageOutboxRetryJobHandler(
        IStorageFallbackService storageFallbackService,
        IStoredFileRepository storedFileRepository,
        IStoredFileService storedFileService,
        IStorageProviderFactory storageProviderFactory,
        ILogger<SuccessfulUploadStorageOutboxRetryJobHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(storageFallbackService, storedFileRepository, storedFileService, storageProviderFactory, logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Context.Type != SuccessfulUploadStorageOutboxRetryJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        await RetrySuccessUploadsAsync(ct);
    }
    
    private async Task RetrySuccessUploadsAsync(CancellationToken ct)
    {
        var successUploadFallbackAsync = await _storageFallbackService
            .GetSuccessUploadFallbackAsync(ct);
        
        if (!successUploadFallbackAsync.Any())
        {
            return;
        }
        
        var successIds = successUploadFallbackAsync
            .Select(f => f.Id)
            .ToList();
        
        var persistentFiles = await _storedFileRepository
            .GetByIdsAsync(successIds, ct);
        
        var pendingFiles = persistentFiles
            .Where(f => f.Status == StorageStatus.Pending)
            .ToList();
        
        
        var uploadedFiles = await HasUploadsAsync(pendingFiles, ct);
        
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
    }
    
    
}