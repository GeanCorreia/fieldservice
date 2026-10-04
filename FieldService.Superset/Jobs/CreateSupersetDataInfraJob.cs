using System.Data.Common;
using FieldService.Data.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKeyById;
using FieldService.SecretKey.Entities;
using FieldService.Shared.Message;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Hangfire;
using MediatR;


namespace FieldService.Superset.Jobs;

internal record CreateSupersetDataInfraJobPayload(
    Guid CreationId,
    Guid TenantId) : AbstractMessagePayload<CreateSupersetDataInfraJobPayload>;

internal record CreateSupersetDataInfraJob : Job<CreateSupersetDataInfraJobPayload>
{
    public static readonly JobType JobType = "superset-create-data-infra-job";

    internal CreateSupersetDataInfraJob(CreateSupersetDataInfraJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal class CreateSupersetDataInfraJobConsumer : IQueueConsumer<CreateSupersetDataInfraJobPayload>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ISupersetTenantFlowRepository _tenantFlowRepository;
    private readonly ISupersetDataBaseService _supersetDataBaseService;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;

    public CreateSupersetDataInfraJobConsumer(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ISupersetTenantFlowRepository tenantFlowRepository,
        ISupersetTenantService supersetTenantService, 
        ISupersetDataBaseService supersetDataBaseService, 
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ISupersetSecretService supersetSecretService)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantFlowRepository = tenantFlowRepository ?? throw new ArgumentNullException(nameof(tenantFlowRepository));
        _supersetDataBaseService = supersetDataBaseService ?? throw new ArgumentNullException(nameof(supersetDataBaseService));
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task ExecuteAsync(Job<CreateSupersetDataInfraJobPayload> job, CancellationToken cancellationToken = default)
    {
        if (job.Context.Type != CreateSupersetDataInfraJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        
        var tenantCreation = await _tenantFlowRepository.GetByIdAsync(job.Payload.CreationId, cancellationToken);
        if (tenantCreation == null)
        {
            throw new InvalidOperationException($"No Superset tenant creation process found for tenant {job.Payload.TenantId}.");
        }
        
        if(tenantCreation.Status == SupersetTenantDeployStatus.Cancelled)
        {
            throw new InvalidOperationException($"Superset tenant creation process for tenant {job.Payload.TenantId} has been cancelled.");
        }
        
        if(tenantCreation.Status == SupersetTenantDeployStatus.Completed)
        {
            throw new InvalidOperationException($"Superset tenant creation process for tenant {job.Payload.TenantId} has already been completed.");
        }
        
        if(tenantCreation.DataSchemaCreatedAt.HasValue)
        {
            throw new InvalidOperationException($"Superset tenant creation process for tenant {job.Payload.TenantId} has already created the data schema.");
        }
        
        var tenantId = job.Payload.TenantId;
        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Could not acquire lock for tenant {tenantId}. " +
                                                $"Another instance creation process might be running.");
        }
        
        DbConnectionStringBuilder? dedicatedDbConnectionString = null;
        
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            if (tenantCreation.CreateParams.DedicatedDbConnectionStringId.HasValue)
            {
                var connectionString = await _mediator.Send(new GetSecretKeyByIdQuery<ConnectionStringSecret>(
                    tenantCreation.CreateParams.DedicatedDbConnectionStringId.Value), cancellationToken);

                dedicatedDbConnectionString = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString?.ConnectionString ??
                                       throw new InvalidOperationException(
                                           $"Dedicated database connection string not found for tenant {tenantId}.")
                };
            }

            var connectionStringId = await _supersetDataBaseService.CreateSupersetDataInfra(
                tenantId,
                dedicatedDbConnectionString,
                cancellationToken: cancellationToken);

            tenantCreation.MarkDataSchemaCreated(connectionStringId);

            await _tenantFlowRepository.SaveAsync(tenantCreation, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
           
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
        }
        
        
    }
    
}

internal class CreateSupersetDataInfraProducer : AbstractPublishProducerWithRequest<CreateSupersetDataInfraJobConsumer, CreateSupersetDataInfraJobPayload>
{
    public CreateSupersetDataInfraProducer(IBackgroundJobClient backgroundJobClient) : base(backgroundJobClient)
    {
    }
    
}
