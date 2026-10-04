using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;

internal sealed record MarkDeletedStorageOutboxRetryJob : Job
{
	public static readonly JobType JobType = "mark-deleted-storage-outbox-retry";

	internal MarkDeletedStorageOutboxRetryJob()
		: base(new JobContext(JobType))
	{
	}
}

internal sealed class MarkDeletedOutboxRetryJobProducer : AbstractScheduleRecurringProducer<MarkDeletedStorageOutboxRetryJobHandler>
{
	public MarkDeletedOutboxRetryJobProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
	{
	}

	protected override Job Job => new MarkDeletedStorageOutboxRetryJob();
	protected override string CronExpression => "13 * * * *";
}

internal class MarkDeletedStorageOutboxRetryJobHandler : AbstractStorageRetryJobService
{
	private readonly IServiceScopeFactory _scopeFactory;

	public MarkDeletedStorageOutboxRetryJobHandler(
		IStorageFallbackService storageFallbackService,
		IStoredFileRepository storedFileRepository,
		IStoredFileService storedFileService,
		IStorageProviderFactory storageProviderFactory,
		ILogger<MarkDeletedStorageOutboxRetryJobHandler> logger,
		IServiceScopeFactory scopeFactory)
		: base(storageFallbackService, storedFileRepository, storedFileService, storageProviderFactory, logger)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
	}

	public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
	{
		if (job.Context.Type != MarkDeletedStorageOutboxRetryJob.JobType)
			throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

		await RetryMarkDeletedAsync(ct);
	}

	private async Task RetryMarkDeletedAsync(CancellationToken ct)
	{
		var fallbackFiles = (await _storageFallbackService.GetFailedMarkDeleteFallbackAsync(ct)).ToList();
		if (fallbackFiles.Count == 0)
		{
			return;
		}

		var fileIds = fallbackFiles.Select(f => f.FileId).ToList();
		var userByFileId = fallbackFiles
			.GroupBy(f => f.FileId)
			.ToDictionary(g => g.Key, g => g.Last().UserId);

		var persistentFiles = (await _storedFileRepository.GetByIdsAsync(fileIds, ct)).ToList();

		var filesToMarkDeleted = persistentFiles
			.Where(file => file.Status != StorageStatus.Deleted && userByFileId.ContainsKey(file.Id))
			.ToList();

		var parallelOptions = new ParallelOptions { CancellationToken = ct };
		await Parallel.ForEachAsync(filesToMarkDeleted, parallelOptions, async (file, cancellationToken) =>
		{
			try
			{
				using var scope = _scopeFactory.CreateScope();
				var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
				await storedFileService.UpdateDeletedStatusAsync(file.Id, userByFileId[file.Id], cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogFailedUploadOutboxServiceError(LogLevel.Error, file.Id, ex);
			}
		});
	}
}

