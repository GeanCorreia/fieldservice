using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Broker;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Logs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantConfigPayload(
    SupersetTenantConfig supersetTenantConfig) : AbstractMessagePayload<CreateSupersetTenantConfigPayload>;
internal record CreateSupersetTenantConfigJob : Job<CreateSupersetTenantConfigPayload>
{
    public static readonly JobType JobType = "superset-tenant-config-persisting-job";

    internal CreateSupersetTenantConfigJob(CreateSupersetTenantConfigPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.supersetTenantConfig.TenantId))
    {
    }
}

internal sealed class CreateSupersetTenantConfigJobProducer : 
    AbstractPublishProducer<CreateSupersetTenantConfigJobConsumer, CreateSupersetTenantConfigPayload>
{
    public CreateSupersetTenantConfigJobProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}

internal class CreateSupersetTenantConfigJobConsumer : IQueueConsumer<CreateSupersetTenantConfigPayload>
{
    
    private readonly ISupersetService _supersetService;
    private readonly ILogger<CreateSupersetTenantConfigJobConsumer> _logger;
    private readonly SupersetTenantDeploymentBrokerProducer _supersetTenantDeploymentBrokerProducer;
    
    public CreateSupersetTenantConfigJobConsumer(
        ISupersetService supersetService, 
        ILogger<CreateSupersetTenantConfigJobConsumer> logger,
        SupersetTenantDeploymentBrokerProducer supersetTenantDeploymentBrokerProducer)
    {
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantDeploymentBrokerProducer = supersetTenantDeploymentBrokerProducer 
                                                  ?? throw new ArgumentNullException(nameof(
                                                      supersetTenantDeploymentBrokerProducer));
    }


    public async Task ExecuteAsync(Job<CreateSupersetTenantConfigPayload> job, CancellationToken ct = default)
    {
        
        if (job.Context.Type != CreateSupersetTenantConfigJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
        var payload = job.Payload;
        
        

        try
        {
            await _supersetService.SaveAsync(payload.supersetTenantConfig, ct);
        }
        catch (Exception ex)
        {
            _logger.LogSupersetTenantConfigCreateError(
                LogLevel.Error,
                payload.supersetTenantConfig.TenantId,
                payload.supersetTenantConfig.ResourceId,
                ex.Message,
                ex);
            
            throw;
        }
        
        var message = new SupersetTenantDeploymentPayload(
            payload.supersetTenantConfig.TenantId,
            payload.supersetTenantConfig.FqdnUrl,
            payload.supersetTenantConfig.ResourceId);

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