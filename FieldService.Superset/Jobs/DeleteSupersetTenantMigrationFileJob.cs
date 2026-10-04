using FieldService.Data.Interfaces;
using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Configuration;
using FieldService.Storage.Interfaces;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Jobs;

internal record DeleteSupersetTenantMigrationFileJob : Job<SupersetTenantMigrationJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-delete-migration-file-job";

    internal DeleteSupersetTenantMigrationFileJob(SupersetTenantMigrationJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal class DeleteSupersetTenantMigrationFileJobHandler : IQueueConsumer<SupersetTenantMigrationJobPayload>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetTenantInstanceProcessingLock _processingLock;
    private readonly ISupersetTenantFlowRepository _repository;
    private readonly IStorageService _storageService;
    private readonly IMediator _mediator;
    private readonly ApplicationAccountOptions _applicationAccountOptions;

    public DeleteSupersetTenantMigrationFileJobHandler(
        IUnitOfWork unitOfWork,
        ISupersetTenantInstanceProcessingLock processingLock,
        ISupersetTenantFlowRepository repository,
        IStorageService storageService,
        IMediator mediator,
        IOptions<ApplicationAccountOptions> applicationAccount)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _processingLock = processingLock ?? throw new ArgumentNullException(nameof(processingLock));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _applicationAccountOptions = applicationAccount.Value ?? throw new ArgumentNullException(nameof(applicationAccount));
    }

    public async Task ExecuteAsync(Job<SupersetTenantMigrationJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != DeleteSupersetTenantMigrationFileJob.JobType)
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
            throw new InvalidOperationException($"No YAML migration file found to delete for tenant {job.Payload.TenantId}.");
        }
        
        
        await _unitOfWork.BeginAsync(ct);
        
        try
        {
            var user = await _mediator.Send(new GetUserTenantQuery(
                _applicationAccountOptions.ServiceUserId,
                _applicationAccountOptions.TenantId), ct);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Service user not found or unauthorized.");
            }

            await _storageService.DeleteAsync(tenantMigration.YamlMigrationFileId.Value, user, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
        finally
        {
            await _processingLock.ReleaseLock(job.Payload.TenantId, ct);
        }
    }
}

internal class DeleteSupersetTenantMigrationFileJobProducerWithRequest : AbstractPublishProducerWithRequest<
    DeleteSupersetTenantMigrationFileJobHandler, SupersetTenantMigrationJobPayload>
{
    public DeleteSupersetTenantMigrationFileJobProducerWithRequest(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}