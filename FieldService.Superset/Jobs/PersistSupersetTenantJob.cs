using FieldService.Queue.Attributes;
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
    Guid DeploymentFlowId,
    Guid TenantId,
    DeploymentType deploymentType) : AbstractMessagePayload<PersistSupersetTenantConfigPayload>;
internal record CreateSupersetTenantConfigJob : Job<PersistSupersetTenantConfigPayload>
{
    public static readonly JobType JobType = "superset-tenant-config-persisting-job";

    internal CreateSupersetTenantConfigJob(PersistSupersetTenantConfigPayload payload)
        : base(payload, new JobContext(
                JobType,
                payload.TenantId,
                SupersetContainerDeploymentFlow.SupersetContainerDeploymentFlowContext(payload.DeploymentFlowId)
            )
        )
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

[DisableRetry]
internal class PersistSupersetTenantConfigJobConsumer : IQueueConsumer<PersistSupersetTenantConfigPayload>
{
    private readonly ISupersetContainerRepository _supersetContainerRepository;
    private readonly ISupersetTenantFlowRepository _supersetTenantFlowRepository;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ILogger<PersistSupersetTenantConfigJobConsumer> _logger;
    private readonly SupersetTenantDeploymentBrokerProducer _supersetTenantDeploymentBrokerProducer;
    
    public PersistSupersetTenantConfigJobConsumer(
        ISupersetContainerRepository supersetContainerRepository,
        ISupersetTenantFlowRepository supersetTenantFlowRepository,
        ISupersetTenantService supersetTenantService, 
        ILogger<PersistSupersetTenantConfigJobConsumer> logger,
        SupersetTenantDeploymentBrokerProducer supersetTenantDeploymentBrokerProducer)
    {
        _supersetContainerRepository = supersetContainerRepository ?? throw new ArgumentNullException(nameof(supersetContainerRepository));
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
        
        var supersetDeploymentFlow = await _supersetTenantFlowRepository.GetByIdAsync(payload.DeploymentFlowId, ct);
        if (supersetDeploymentFlow == null)
        {
            throw new SupersetTenantFlowNotFoundException(payload.DeploymentFlowId);
        }
        
        if(supersetDeploymentFlow.TenantId != job.Payload.TenantId)
            throw new InvalidOperationException($"Tenant ID mismatch for creation process. Expected: {supersetDeploymentFlow.TenantId}, Actual: {job.Payload.TenantId}.");

        
        if(supersetDeploymentFlow.Status != SupersetTenantDeployStatus.Completed)
        {
            throw new InvalidOperationException($"Superset tenant flow '{payload.DeploymentFlowId}' is not completed. Current status: {supersetDeploymentFlow.Status}");
        }

        SupersetTenant? supersetTenant = null;
        
        if (supersetDeploymentFlow.ContainerId == null)
        {
            throw new InvalidOperationException("SupersetDeploymentFlow Creation without ContainerId");
        }

        var container =
            await _supersetContainerRepository.GetSupersetContainerByIdAsync(supersetDeploymentFlow.ContainerId
                .Value);

        if (container == null)
        {
            throw new SupersetContainerNotFoundException(supersetDeploymentFlow.ContainerId.Value);
        }

        if (container.TenantId != supersetDeploymentFlow.TenantId)
        {
            throw new InvalidOperationException();
        }

        if (supersetDeploymentFlow.ConnectionStringId == null)
        {
            throw new InvalidOperationException($"SupersetDeploymentFlow Id {supersetDeploymentFlow.Id}" +
                                                $" without ConnectionStringId.");
        }

        supersetTenant = SupersetTenant.Create(
            tenantId: job.Payload.TenantId,
            supersetContainer: container,
            supersetDeploymentFlow.ConnectionStringId.Value
        );
        
        
        var message = new SupersetTenantDeploymentPayload(
            supersetTenant.TenantId,
            supersetTenant.FqdnUrl,
            supersetTenant.Container.ResourceId);
        
        await _supersetTenantService.SaveAsync(supersetTenant, ct);

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
    