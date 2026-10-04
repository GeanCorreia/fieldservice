using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Broker;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Logs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Jobs;

internal enum DeploymentType
{
    Create,
    Update
}

internal record PersistSupersetTenantConfigPayload(
    Guid SupersetTenantFlowId,
    Guid TenantId,
    DeploymentType deploymentType) : AbstractMessagePayload<PersistSupersetTenantConfigPayload>;
internal record CreateSupersetTenantConfigJob : Job<PersistSupersetTenantConfigPayload>
{
    public static readonly JobType JobType = "superset-tenant-config-persisting-job";

    internal CreateSupersetTenantConfigJob(PersistSupersetTenantConfigPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal sealed class PersistSupersetTenantConfigProducerWithRequest : 
    AbstractPublishProducerWithRequest<PersistSupersetTenantConfigJobConsumer, PersistSupersetTenantConfigPayload>
{
    public PersistSupersetTenantConfigProducerWithRequest(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}

internal class PersistSupersetTenantConfigJobConsumer : IQueueConsumer<PersistSupersetTenantConfigPayload>
{
    private readonly ISupersetTenantFlowService _supersetTenantFlowService;
    private readonly ISupersetTenantFlowRepository _supersetTenantFlowRepository;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ILogger<PersistSupersetTenantConfigJobConsumer> _logger;
    private readonly SupersetTenantDeploymentBrokerProducer _supersetTenantDeploymentBrokerProducer;
    
    public PersistSupersetTenantConfigJobConsumer(
        ISupersetTenantFlowService supersetTenantFlowService,
        ISupersetTenantFlowRepository supersetTenantFlowRepository,
        ISupersetTenantService supersetTenantService, 
        ILogger<PersistSupersetTenantConfigJobConsumer> logger,
        SupersetTenantDeploymentBrokerProducer supersetTenantDeploymentBrokerProducer)
    {
        _supersetTenantFlowService = supersetTenantFlowService ?? throw new ArgumentNullException(nameof(supersetTenantFlowService));
        _supersetTenantFlowRepository = supersetTenantFlowRepository ?? throw new ArgumentNullException(nameof(supersetTenantFlowRepository));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantDeploymentBrokerProducer = supersetTenantDeploymentBrokerProducer 
                                                  ?? throw new ArgumentNullException(nameof(
                                                      supersetTenantDeploymentBrokerProducer));
    }


    public async Task ExecuteAsync(Job<PersistSupersetTenantConfigPayload> job, CancellationToken ct = default)
    {
        
        if (job.Context.Type != CreateSupersetTenantConfigJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
        var payload = job.Payload;
        
        var supersetTenantFlow = await _supersetTenantFlowRepository.GetByIdAsync(payload.SupersetTenantFlowId, ct);
        if (supersetTenantFlow == null)
        {
            throw new SupersetTenantFlowNotFoundException(payload.SupersetTenantFlowId);
        }
        
        if(supersetTenantFlow.Status != SupersetTenantDeployStatus.Completed)
        {
            throw new InvalidOperationException($"Superset tenant flow '{payload.SupersetTenantFlowId}' is not completed. Current status: {supersetTenantFlow.Status}");
        }

        SupersetTenant? supersetTenant = null;
        
        if(job.Payload.deploymentType == DeploymentType.Create)
        {
            supersetTenant = await _supersetTenantFlowService.BuildSupersetTenantCreationFlowAsync(job.Payload.SupersetTenantFlowId, ct);
        }
        else
        {
            supersetTenant = await _supersetTenantFlowService.BuildSupersetTenantUpdateFlowAsync(job.Payload.SupersetTenantFlowId, ct);
        }
        
        var message = new SupersetTenantDeploymentPayload(
            supersetTenant.TenantId,
            supersetTenant.FqdnUrl,
            supersetTenant.Container.ResourceId);

        try
        {
            await _supersetTenantDeploymentBrokerProducer.PublishAsync(message, ct);
        }
        catch (Exception ex)
        {
            //no exception should be thrown here, as the deployment notice is not critical for the tenant creation
        }
    }
    
    
}