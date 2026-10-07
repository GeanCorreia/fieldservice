using FieldService.Data.Interfaces;
using FieldService.Queue.Attributes;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Hangfire;


namespace FieldService.Superset.Jobs;

internal record CreateSupersetDataInfraJobPayload(
    Guid FlowId,
    Guid TenantId) : AbstractMessagePayload<CreateSupersetDataInfraJobPayload>;

internal record CreateSupersetDataInfraJob : Job<CreateSupersetDataInfraJobPayload>
{
    public static readonly JobType JobType = "superset-create-data-infra-job";

    internal CreateSupersetDataInfraJob(CreateSupersetDataInfraJobPayload payload)
        : base(payload, new JobContext(
            JobType,
            payload.TenantId,
            SupersetContainerDeploymentFlow.SupersetContainerDeploymentFlowContext(payload.FlowId)
            )
        )
    {
    }
}

[DisableRetry]
internal class CreateSupersetDataInfraJobConsumer : IQueueConsumer<CreateSupersetDataInfraJobPayload>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetTenantFlowRepository _tenantFlowRepository;
    private readonly ISupersetDataBaseService _supersetDataBaseService;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;

    public CreateSupersetDataInfraJobConsumer(
        IUnitOfWork unitOfWork,
        ISupersetTenantFlowRepository tenantFlowRepository,
        ISupersetDataBaseService supersetDataBaseService, 
        ISupersetTenantInstanceProcessingLock supersetInstanceLock)
    {
        _unitOfWork = unitOfWork;
        _tenantFlowRepository = tenantFlowRepository ?? throw new ArgumentNullException(nameof(tenantFlowRepository));
        _supersetDataBaseService = supersetDataBaseService ?? throw new ArgumentNullException(nameof(supersetDataBaseService));
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task ExecuteAsync(Job<CreateSupersetDataInfraJobPayload> job, CancellationToken cancellationToken = default)
    {
        if (job.Context.Type != CreateSupersetDataInfraJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        
        var supersetDeploymentFlow = await _tenantFlowRepository.GetByIdAsync(job.Payload.FlowId, cancellationToken);
        if (supersetDeploymentFlow == null)
        {
            throw new InvalidOperationException($"No Superset tenant creation process found for tenant {job.Payload.TenantId}.");
        }
        
        if(supersetDeploymentFlow.Status == SupersetTenantDeployStatus.Cancelled)
        {
            throw new InvalidOperationException($"Superset tenant creation process for tenant {job.Payload.TenantId} has been cancelled.");
        }
        
        if(supersetDeploymentFlow.Status == SupersetTenantDeployStatus.Completed)
        {
            return;
        }
        
        if(supersetDeploymentFlow.DataSchemaCreatedAt.HasValue)
        {
            return;
        }

        if (supersetDeploymentFlow.ConnectionStringId.HasValue)
        {
            await _supersetDataBaseService.EnsureSupersetDataInfraCheckpointAsync(
                tenantId: job.Payload.TenantId,
                connectionStringId: supersetDeploymentFlow.ConnectionStringId.Value,
                cancellationToken: cancellationToken);
            return;
        }
        
        if(supersetDeploymentFlow.TenantId != job.Payload.TenantId)
            throw new InvalidOperationException($"Tenant ID mismatch for creation process. Expected: {supersetDeploymentFlow.TenantId}, Actual: {job.Payload.TenantId}.");
        
        var tenantId = job.Payload.TenantId;
        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Could not acquire lock for tenant {tenantId}. " +
                                                $"Another instance creation process might be running.");
        }
        
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {

            var connectionStringId = await _supersetDataBaseService.CreateSupersetDataInfra(
                tenantId,
                supersetDeploymentFlow.CustomHostConnectionStringId,
                cancellationToken: cancellationToken);

            supersetDeploymentFlow.MarkDataSchemaCreated(connectionStringId);

            await _tenantFlowRepository.SaveAsync(supersetDeploymentFlow, cancellationToken);
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
