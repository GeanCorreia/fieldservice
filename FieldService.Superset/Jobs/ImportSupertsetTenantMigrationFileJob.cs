using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Configuration;
using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Options;
using Refit;

namespace FieldService.Superset.Jobs;

internal record ImportSupersetTenantMigrationFileJob : Job<SupersetTenantMigrationJobPayload>
{
	public static readonly JobType JobType = "superset-tenant-import-migration-file-job";

	internal ImportSupersetTenantMigrationFileJob(SupersetTenantMigrationJobPayload payload)
		: base(payload, new JobContext(JobType, tenantId: payload.TenantId))
	{
	}
}

internal class ImportSupersetTenantMigrationFileJobHandler : IQueueConsumer<SupersetTenantMigrationJobPayload>
{
	private readonly ISupersetTenantService _supersetTenantService;
	private readonly ISupersetContainerRepository _containerRepository;
	private readonly ISupersetTenantInstanceProcessingLock _processingLock;
	private readonly ISupersetTenantFlowRepository _repository;
	private readonly ISupersetSecurityApi _supersetSecurityApi;
	private readonly ISupersetApi _supersetApi;
	private readonly IStorageService _storageService;
	private readonly IMediator _mediator;
	private readonly ApplicationAccountOptions _applicationAccountOptions;
	private readonly SupersetOptions _supersetOptions;

	public ImportSupersetTenantMigrationFileJobHandler(
		ISupersetTenantService supersetTenantService,
		ISupersetContainerRepository containerRepository,
		ISupersetTenantInstanceProcessingLock processingLock,
		ISupersetTenantFlowRepository repository,
		ISupersetSecurityApi supersetSecurityApi,
		ISupersetApi supersetApi,
		IStorageService storageService,
		IMediator mediator,
		IOptions<SupersetOptions> supersetOptions,
		IOptions<ApplicationAccountOptions> applicationAccount)
	{
		_supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
		_containerRepository = containerRepository ?? throw new ArgumentNullException(nameof(containerRepository));
		_processingLock = processingLock ?? throw new ArgumentNullException(nameof(processingLock));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
		_supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
		_storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_supersetOptions = supersetOptions.Value ?? throw new ArgumentNullException(nameof(supersetOptions));
		_applicationAccountOptions = applicationAccount.Value ?? throw new ArgumentNullException(nameof(applicationAccount));
	}

	public async Task ExecuteAsync(Job<SupersetTenantMigrationJobPayload> job, CancellationToken ct = default)
	{
		if (job.Context.Type != ImportSupersetTenantMigrationFileJob.JobType)
			throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

		var tenantMigration = await _repository.GetByIdAsync(job.Payload.MigrationId, ct);
		if (tenantMigration == null)
		{
			throw new InvalidOperationException($"No Superset tenant migration process found for tenant {job.Payload.TenantId}.");
		}

		if (tenantMigration.FlowType != FlowType.Migration)
		{
			throw new InvalidOperationException($"Superset tenant migration process for tenant {job.Payload.TenantId} is not a migration process.");
		}

		if (tenantMigration.TenantId != job.Payload.TenantId)
		{
			throw new InvalidOperationException($"Tenant ID mismatch for migration process. Expected: {tenantMigration.TenantId}, Actual: {job.Payload.TenantId}.");
		}

		if (!tenantMigration.YamlMigrationFileId.HasValue)
		{
			throw new InvalidOperationException($"No YAML migration file found to import for tenant {job.Payload.TenantId}.");
		}
		
		if (!tenantMigration.CreatedContainerId.HasValue)
		{
			throw new InvalidOperationException($"No created container found for tenant {job.Payload.TenantId}.");
		} 
		
		var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(job.Payload.TenantId, ct);
		
		if (supersetTenant == null)
		{
			throw new SupersetTenantNotFoundException(job.Payload.TenantId);
		}

		if (!await _processingLock.AcquireLock(job.Payload.TenantId, ct))
		{
			throw new InvalidOperationException($"Failed to acquire processing lock for tenant {job.Payload.TenantId}.");
		}

		try
		{

			var containerId = tenantMigration.CreatedContainerId.Value;
			var container = await _containerRepository.GetSupersetContainerByIdAsync(containerId, ct);
			if (container == null)
			{
				throw new SupersetContainerNotFoundException(containerId);
			}
			
			var user = await _mediator.Send(new GetUserTenantQuery(
				_applicationAccountOptions.ServiceUserId,
				_applicationAccountOptions.TenantId), ct);

			if (user == null)
			{
				throw new UnauthorizedAccessException("Service user not found or unauthorized.");
			}

			var loginResponse = await _supersetSecurityApi.LoginAsync(
				new Uri(container.FqdnUrl),
				new SupersetLoginApiRequest(_supersetOptions.Username, _supersetOptions.Password),
				ct);

			if (string.IsNullOrWhiteSpace(loginResponse.AccessToken))
			{
				throw new InvalidOperationException("Superset admin token not found.");
			}

			var download = await _storageService.DownloadAsync(
				new StoredFileDownloadRequest(tenantMigration.YamlMigrationFileId.Value, user),
				ct);

			if (download.Content.CanSeek)
			{
				download.Content.Position = 0;
			}

			var streamPart = new StreamPart(
				download.Content,
				download.File.FileName,
				download.File.ContentType.MediaType);

			await _supersetApi.ImportAssetsAsync(
				new Uri(container.FqdnUrl),
				$"Bearer {loginResponse.AccessToken}",
				streamPart,
				overwrite: true,
				cancellationToken: ct);
		}
		finally
		{
			await _processingLock.ReleaseLock(job.Payload.TenantId, ct);
		}
	}
}

internal class ImportSupersetTenantMigrationFileJobProducerWithRequest : AbstractPublishProducerWithRequest<
	ImportSupersetTenantMigrationFileJobHandler, SupersetTenantMigrationJobPayload>
{
	public ImportSupersetTenantMigrationFileJobProducerWithRequest(IBackgroundJobClient backgroundJobClient)
		: base(backgroundJobClient)
	{
	}
}

