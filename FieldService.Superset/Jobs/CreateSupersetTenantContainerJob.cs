using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;
using Hangfire;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantContainerJobPayload(
    SupersetTenantCreateParams supersetTenantCreateParams,
    Guid connectionStringId,
    Guid? userId = null) : 
    AbstractMessagePayload<CreateSupersetTenantContainerJobPayload>;

internal record CreateSupersetTenantContainerJob : Job<CreateSupersetTenantContainerJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-container-create-job";

    internal CreateSupersetTenantContainerJob(CreateSupersetTenantContainerJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.supersetTenantCreateParams.TenantId))
    {
    }
}

internal class CreateSupersetTenantContainerJobConsumer : IQueueConsumer<CreateSupersetTenantContainerJobPayload>
{
    private readonly ISupersetTenantDeploymentService _supersetTenantDeploymentService;
    private readonly CreateSupersetTenantConfigJobProducer _createSupersetTenantConfigJobProducer;

    public CreateSupersetTenantContainerJobConsumer(
        ISupersetTenantDeploymentService supersetTenantDeploymentService,
        CreateSupersetTenantConfigJobProducer createSupersetTenantConfigJobProducer)
    {
        _supersetTenantDeploymentService = supersetTenantDeploymentService ?? throw new ArgumentNullException(nameof(supersetTenantDeploymentService));
        _createSupersetTenantConfigJobProducer = createSupersetTenantConfigJobProducer ?? throw new ArgumentNullException(nameof(createSupersetTenantConfigJobProducer));
    }

    public async Task ExecuteAsync(Job<CreateSupersetTenantContainerJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != CreateSupersetTenantContainerJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var payload = job.Payload;
    
        await _supersetTenantDeploymentService.CreateInstanceAsync(
            payload.supersetTenantCreateParams, 
            payload.connectionStringId,
            payload.userId,
            ct);

    }
    
    internal class CreateSupersetTenantContainerJobProducer : AbstractPublishProducer<CreateSupersetTenantContainerJobConsumer,CreateSupersetTenantContainerJobPayload>
    {
        public CreateSupersetTenantContainerJobProducer(IBackgroundJobClient backgroundJobClient) : base(backgroundJobClient)
        {
        }
    }
}
