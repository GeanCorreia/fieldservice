using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantContainerJobPayload(
    SupersetTenantCreateParams SupersetTenantCreateParams) : 
    AbstractMessagePayload<CreateSupersetTenantContainerJobPayload>;

internal record CreateSupersetTenantContainerJob : Job<CreateSupersetTenantContainerJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-container-create-job";

    internal CreateSupersetTenantContainerJob(CreateSupersetTenantContainerJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.SupersetTenantCreateParams.TenantId))
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
    
        var result = await _supersetTenantDeploymentService.CreateInstanceAsync(
            payload.SupersetTenantCreateParams, 
            ct);

        var jobPayload = new CreateSupersetTenantConfigPayload(result);
        var newJob = new CreateSupersetTenantConfigJob(jobPayload);
        _createSupersetTenantConfigJobProducer.Publish(newJob);
        
    }
}
