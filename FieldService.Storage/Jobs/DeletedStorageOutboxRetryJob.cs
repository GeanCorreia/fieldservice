using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Jobs;

internal sealed record DeletedStorageOutboxRetryJob : Job
{
	public static readonly JobType JobType = "deleted-storage-outbox-retry";

	internal DeletedStorageOutboxRetryJob()
		: base(new JobContext(JobType))
	{
	}
}

internal sealed class DeletedOutboxRetryJobProducer : AbstractScheduleRecurringProducer<DeletedStorageOutboxRetryJobHandler>
{
	public DeletedOutboxRetryJobProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
	{
	}

	protected override Job Job => new DeletedStorageOutboxRetryJob();
	protected override string CronExpression => "23 * * * *";
}

internal class DeletedStorageOutboxRetryJobHandler : AbstractStorageRetryJobService
{
	private readonly IServiceScopeFactory _scopeFactory;

	public DeletedStorageOutboxRetryJobHandler(
		IStorageFallbackService storageFallbackService,
		IStoredFileRepository storedFileRepository,
		IStoredFileService storedFileService,
		IStorageProviderFactory storageProviderFactory,
		ILogger<DeletedStorageOutboxRetryJobHandler> logger,
		IServiceScopeFactory scopeFactory)
		: base(storageFallbackService, storedFileRepository, storedFileService, storageProviderFactory, logger)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
	}

	public override async Task ExecuteAsync(Job job, CancellationToken ct = default)
	{
		if (job.Context.Type != DeletedStorageOutboxRetryJob.JobType)
			throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

		await RetryDeleteAsync(ct);
	}

	private async Task RetryDeleteAsync(CancellationToken ct)
	{
		var fallbackFiles = (await _storageFallbackService.GetFailedDeleteFallbackAsync(ct)).ToList();
		if (!fallbackFiles.Any())
		{
			return;
		}

		var fileIds = fallbackFiles.Select(f => f.FileId).ToList();
		var userByFileId = fallbackFiles
			.GroupBy(f => f.FileId)
			.ToDictionary(g => g.Key, g => g.Last().UserId);

		var persistentFiles = (await _storedFileRepository.GetByIdsAsync(fileIds, ct)).ToList();
		if (!persistentFiles.Any())
		{
			return;
		}

		var parallelOptions = new ParallelOptions { CancellationToken = ct };

		var readyFiles = persistentFiles.Where(file => userByFileId.ContainsKey(file.Id)).ToList();
		if (!readyFiles.Any())
		{
			return;
		}

		var uploadedFiles = (await HasUploadsAsync(readyFiles, ct)).ToList();
		var deletedFileIds = (await DeleteFilesFromStorageAsync(uploadedFiles, ct)).ToList();

		var uploadedIdSet = uploadedFiles.Select(file => file.Id).ToHashSet();
		var resolvedIds = deletedFileIds
			.Concat(readyFiles.Where(file => !uploadedIdSet.Contains(file.Id)).Select(file => file.Id))
			.Distinct()
			.ToList();

		await Parallel.ForEachAsync(resolvedIds, parallelOptions, async (fileId, cancellationToken) =>
		{
			try
			{
				using var scope = _scopeFactory.CreateScope();
				var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
				await storedFileService.UpdateDeletedStatusAsync(fileId, userByFileId[fileId], cancellationToken);
			}
			catch (Exception ex)
			{
				await _storageFallbackService.CreateFallbackFailedMarkDeleteCache(fileId, userByFileId[fileId], cancellationToken);
				_logger.LogFailedUploadOutboxServiceError(LogLevel.Error, fileId, ex);
			}
		});

		await Parallel.ForEachAsync(resolvedIds, parallelOptions, async (fileId, cancellationToken) =>
		{
			try
			{
				await _storageFallbackService.RemoveFallbackCached(fileId, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogFailedUploadOutboxServiceError(LogLevel.Error, fileId, ex);
			}
		});
	}
}

