using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Superset.Broker;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Hangfire;

namespace FieldService.Superset.Jobs;

internal sealed record StartSupersetTenantInstanceJob : Job<Guid>
{
    public static readonly JobType JobType = "superset-start-tenant-instance-job";
    
    internal StartSupersetTenantInstanceJob(Guid tenantId)
        : base(tenantId,
            new JobContext(JobType, tenantId: tenantId) 
            )
    {
    }
}

internal sealed class StartSupersetTenantInstanceJobHandler : IQueueConsumer<Guid>
{
    private readonly ISupersetService _supersetService;
    private readonly ISupersetTenantInstanceLifecycleService _supersetTenantInstanceLifecycleService;
    private readonly SupersetTenantInstanceStartedBrokerProducer _supersetTenantInstanceStartedBrokerProducer;

    public StartSupersetTenantInstanceJobHandler(
        ISupersetService supersetService,
        ISupersetTenantInstanceLifecycleService supersetTenantInstanceLifecycleService,
        SupersetTenantInstanceStartedBrokerProducer supersetTenantInstanceStartedBrokerProducer)    
    {
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _supersetTenantInstanceLifecycleService = supersetTenantInstanceLifecycleService ?? throw new ArgumentNullException(nameof(supersetTenantInstanceLifecycleService));
        _supersetTenantInstanceStartedBrokerProducer = supersetTenantInstanceStartedBrokerProducer ?? throw new ArgumentNullException(nameof(supersetTenantInstanceStartedBrokerProducer));
    }

    public async Task ExecuteAsync(Job<Guid> job, CancellationToken ct = default)
    {
        if (job.Context.Type != StartSupersetTenantInstanceJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        await _supersetTenantInstanceLifecycleService.ScaleUpContainerAsync(job.Payload, ct);

        try
        {
            var supersetTenantConfig = await _supersetService.GetSupersetTenantByTenantIdAsync(
                job.Payload, 
                ct);
            
            if(supersetTenantConfig == null)
               return;
        
            var brokerPayload = new SupersetTenantInstanceStartedPayload(
                TenantId: supersetTenantConfig.TenantId,
                FqdnUrl: supersetTenantConfig.FqdnUrl,
                AzureResourceId: supersetTenantConfig.ResourceId
            );

            await _supersetTenantInstanceStartedBrokerProducer.PublishAsync(brokerPayload, ct);
        }
        catch 
        {
            //No exception should be thrown if the broker message fails to send, as the main
            //job of starting the tenant instance has already been completed. Log the error if necessary.
        }
        
    }
    
    
}

internal sealed class StartSupersetTenantInstanceCreatedJobProducer : AbstractPublishProducer<StartSupersetTenantInstanceJobHandler, Guid>
{
    public StartSupersetTenantInstanceCreatedJobProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}