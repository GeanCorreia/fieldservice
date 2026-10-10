using FieldService.Data.Interfaces;
using FieldService.Queue.Attributes;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Shared.Types;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantContainerJobPayload(
    Guid FlowId,
    Guid TenantId) : 
    AbstractMessagePayload<CreateSupersetTenantContainerJobPayload>;

internal record CreateSupersetTenantContainerJob : Job<CreateSupersetTenantContainerJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-container-create-job";

    internal CreateSupersetTenantContainerJob(CreateSupersetTenantContainerJobPayload payload)
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
internal class CreateSupersetTenantContainerJobConsumer : IQueueConsumer<CreateSupersetTenantContainerJobPayload>
{
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetTenantFlowRepository _repository;
    private readonly ISupersetContainerDeploymentService _supersetContainerDeploymentService;
    private readonly ISupersetContainerRepository _supersetContainerRepository;
    private readonly ILogger<CreateSupersetTenantContainerJobConsumer> _logger;

    public CreateSupersetTenantContainerJobConsumer(
        ISupersetTenantService supersetTenantService,
        IUnitOfWork unitOfWork,
        ISupersetTenantFlowRepository repository,
        ISupersetContainerDeploymentService supersetContainerDeploymentService,
        ISupersetContainerRepository supersetContainerRepository,
        ILogger<CreateSupersetTenantContainerJobConsumer> logger)
    {
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _supersetContainerDeploymentService = supersetContainerDeploymentService ?? 
                                           throw new ArgumentNullException(nameof(supersetContainerDeploymentService));
        _supersetContainerRepository = supersetContainerRepository 
                                       ?? throw new ArgumentNullException(nameof(supersetContainerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
       
    }

    public async Task ExecuteAsync(Job<CreateSupersetTenantContainerJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != CreateSupersetTenantContainerJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var payload = job.Payload;

        var supersetDeploymentFlow = await _repository.GetByIdAsync(payload.FlowId, ct);
        
        if (supersetDeploymentFlow == null)
        {
            throw new InvalidOperationException($"SupersetTenantCreation with ID {payload.FlowId} not found.");
        }
        
        if(supersetDeploymentFlow.TenantId != payload.TenantId)
            throw new InvalidOperationException($"Tenant ID mismatch for creation process. Expected: {supersetDeploymentFlow.TenantId}, Actual: {payload.TenantId}.");
        
        
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(supersetDeploymentFlow.TenantId, ct);
        if (supersetTenant != null)
        {
            return;
        }
        
        if(supersetDeploymentFlow.ConnectionStringId == null)
        {
            throw new InvalidOperationException($"Connection string ID for creation process is null. Creation ID: {supersetDeploymentFlow.Id}");
        }

        if (supersetDeploymentFlow.SecretKeyId == null)
        {
            throw new InvalidOperationException($"Secret key ID for creation process is null. Creation ID: {supersetDeploymentFlow.Id}");
        }
        
        
        
        var previousContainer = await _supersetContainerRepository.GetSupersetContainerByTenantIdAsync(payload.TenantId, ct);
        var containerId = previousContainer?.Id ?? payload.TenantId;

        SupersetContainerDeploymentResult deploymentResult;
        try
        {
            deploymentResult = await _supersetContainerDeploymentService.CreateInstanceAsync(
                payload.TenantId,
                supersetDeploymentFlow.Configuration,
                supersetDeploymentFlow.ConnectionStringId.Value,
                supersetDeploymentFlow.SecretKeyId.Value,
                ct);
        }
        catch (SupersetContainerResourceAlreadyExistsException ex)
        {
            _logger.LogInformation(
                "Superset container resource already exists for tenant {TenantId}: {ResourceId}. Continuing persistence flow.",
                payload.TenantId,
                ex.ResourceId);

            deploymentResult = new SupersetContainerDeploymentResult(ex.ResourceId, ex.FqdnUrl);
        }

        var supersetContainer = new SupersetContainer(
            id: containerId,
            tenantId: payload.TenantId,
            cloudProvider: CloudProvider.Azure,
            secretKeyId: supersetDeploymentFlow.SecretKeyId.Value,
            resourceId: deploymentResult.ResourceId,
            fqdnUrl: deploymentResult.FqdnUrl,
            executionType: supersetDeploymentFlow.Configuration.ExecutionType,
            maxReplicas: supersetDeploymentFlow.Configuration.MaxReplicas,
            minReplicas: supersetDeploymentFlow.Configuration.MinReplicas,
            status: ContainerStatus.Active,
            executionWindow: supersetDeploymentFlow.Configuration.ExecutionWindow);

        var mustPersistContainer = previousContainer is null
                                   || previousContainer.SecretKeyId != supersetContainer.SecretKeyId
                                   || !string.Equals(previousContainer.ResourceId, supersetContainer.ResourceId, StringComparison.Ordinal)
                                   || !string.Equals(previousContainer.FqdnUrl, supersetContainer.FqdnUrl, StringComparison.Ordinal)
                                   || previousContainer.Status != ContainerStatus.Active
                                   || !previousContainer.Configuration.Equals(supersetContainer.Configuration);

        var mustPersistFlow = !supersetDeploymentFlow.ContainerCreatedAt.HasValue
                              || supersetDeploymentFlow.ContainerId != supersetContainer.Id
                              || supersetDeploymentFlow.SecretKeyId != supersetContainer.SecretKeyId;

        if (!mustPersistContainer && !mustPersistFlow)
            return;
        
        if (mustPersistFlow)
        {
            supersetDeploymentFlow.MarkContainerCreated(
                supersetContainer.Id);
        }
        
        await _unitOfWork.BeginAsync(ct);

        try
        {
            
            if (mustPersistContainer)
            {
                await _supersetContainerRepository.SaveSupersetContainerAsync(supersetContainer, ct);
            }

            if (mustPersistFlow)
            {
                await _repository.SaveAsync(supersetDeploymentFlow, ct);
            }
            
            await _unitOfWork.CommitAsync(ct);
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
