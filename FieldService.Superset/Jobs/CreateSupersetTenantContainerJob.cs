using FieldService.Data.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Hangfire;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantContainerJobPayload(
    Guid CreationId,
    Guid TenantId,
    SupersetTenantCreateParams supersetTenantCreateParams,
    Guid connectionStringId,
    Guid? userId = null) : 
    AbstractMessagePayload<CreateSupersetTenantContainerJobPayload>;

internal record CreateSupersetTenantContainerJob : Job<CreateSupersetTenantContainerJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-container-create-job";

    internal CreateSupersetTenantContainerJob(CreateSupersetTenantContainerJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal class CreateSupersetTenantContainerJobConsumer : IQueueConsumer<CreateSupersetTenantContainerJobPayload>
{
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetTenantFlowRepository _repository;
    private readonly ISupersetContainerDeploymentService _supersetContainerDeploymentService;
    private readonly PersistSupersetTenantConfigProducerWithRequest _persistSupersetTenantConfigProducerWithRequest;

    public CreateSupersetTenantContainerJobConsumer(
        ISupersetTenantService supersetTenantService,
        IUnitOfWork unitOfWork,
        ISupersetTenantFlowRepository repository,
        ISupersetContainerDeploymentService supersetContainerDeploymentService,
        PersistSupersetTenantConfigProducerWithRequest persistSupersetTenantConfigProducerWithRequest)
    {
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _supersetContainerDeploymentService = supersetContainerDeploymentService ?? 
                                           throw new ArgumentNullException(nameof(supersetContainerDeploymentService));
        _persistSupersetTenantConfigProducerWithRequest = persistSupersetTenantConfigProducerWithRequest ?? throw new ArgumentNullException(nameof(persistSupersetTenantConfigProducerWithRequest));
    }

    public async Task ExecuteAsync(Job<CreateSupersetTenantContainerJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != CreateSupersetTenantContainerJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var payload = job.Payload;

        var creationRequest = await _repository.GetByIdAsync(payload.CreationId, ct);
        
        if (creationRequest == null)
        {
            throw new InvalidOperationException($"SupersetTenantCreation with ID {payload.CreationId} not found.");
        }
        
        if(creationRequest.TenantId != payload.TenantId)
            throw new InvalidOperationException($"Tenant ID mismatch for creation process. Expected: {creationRequest.TenantId}, Actual: {payload.TenantId}.");
        
        if(creationRequest.CreateParams.ExecutionType == ExecutionType.OnDemand)
        {
           throw new InvalidOperationException($"SupersetTenantCreation with ID {payload.CreationId} is of type " +
                                               $"OnDemand and should not be create a container.");
        }
        
        if(creationRequest.FlowType == FlowType.Migration)
        {
            var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(creationRequest.TenantId, ct);
            if (supersetTenant == null)
            {
                throw new SupersetTenantNotFoundException(creationRequest.TenantId);
            }
            
            if(supersetTenant.Container.ExecutionType == ExecutionType.AlwaysOn || 
               supersetTenant.Container.ExecutionType == ExecutionType.Scheduled)
            {
                throw new InvalidOperationException($"SupersetTenantCreation with ID {payload.CreationId} is of type " +
                                                    $"{creationRequest.FlowType}. " + $"Tenant already has a container " +
                                                    $"with execution type " +
                                                    $"{supersetTenant.Container.ExecutionType}. " +
                                                    $"Cannot create a new container.");
            }
            
        }
        
        await _unitOfWork.BeginAsync(ct);

        try
        {
            var supersetContainer = await _supersetContainerDeploymentService.CreateInstanceAsync(
                payload.TenantId,
                payload.supersetTenantCreateParams, 
                payload.connectionStringId,
                payload.userId,
                ct);
            
            creationRequest.MarkContainerCreated(
                supersetContainer.Id, 
                supersetContainer.SecretKeyId);
            
            await _repository.SaveAsync(creationRequest, ct);
            
        }
        catch 
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
        
    }
    
    
}

internal class CreateSupersetTenantContainerJobProducer : AbstractPublishProducerWithRequest<CreateSupersetTenantContainerJobConsumer,CreateSupersetTenantContainerJobPayload>
{
    public CreateSupersetTenantContainerJobProducer(IBackgroundJobClient backgroundJobClient) : base(backgroundJobClient)
    {
    }
}
