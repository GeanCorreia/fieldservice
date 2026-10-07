using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Entities;
using Hangfire;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantFlowJobPayload(
    Guid FlowId,
    Guid TenantId) : AbstractMessagePayload<CreateSupersetTenantFlowJobPayload>;

internal record CreateSupersetTenantFlowJob : Job<CreateSupersetTenantFlowJobPayload>
{
    public static readonly JobType JobType = "superset-create-tenant-job";
    public CreateSupersetTenantFlowJob(CreateSupersetTenantFlowJobPayload payload) 
        : base(payload, new JobContext(
                JobType,
                payload.TenantId,
                SupersetContainerDeploymentFlow.SupersetContainerDeploymentFlowContext(payload.FlowId)
            )
        )
    {
    }
}

internal sealed class CreateSupersetTenantFlowProducer : AbstractPublishFlowProducerWithRequest
{
    public CreateSupersetTenantFlowProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string PublishAsync(CreateSupersetTenantFlowJobPayload jobPayload)
    {
        ArgumentNullException.ThrowIfNull(jobPayload);

        var secretKeyJobId = EnqueueFirst<CreateSupersetTenantSecretKeyJobConsumer, CreateSupersetTenantSecretKeyJobPayload>(
            new CreateSupersetTenantSecretKeyJob(new CreateSupersetTenantSecretKeyJobPayload(jobPayload.FlowId, jobPayload.TenantId)));

        var dataInfraJobId = ContinueWith<CreateSupersetDataInfraJobConsumer, CreateSupersetDataInfraJobPayload>(
            secretKeyJobId,
            new CreateSupersetDataInfraJob(new CreateSupersetDataInfraJobPayload(jobPayload.FlowId, jobPayload.TenantId)));

        var containerJobId = ContinueWith<CreateSupersetTenantContainerJobConsumer, CreateSupersetTenantContainerJobPayload>(
            dataInfraJobId,
            new CreateSupersetTenantContainerJob(new CreateSupersetTenantContainerJobPayload(jobPayload.FlowId, jobPayload.TenantId))); 

        return ContinueWith<PersistSupersetTenantConfigJobConsumer, PersistSupersetTenantConfigPayload>(
            containerJobId,
            new CreateSupersetTenantConfigJob(new PersistSupersetTenantConfigPayload(jobPayload.FlowId, jobPayload.TenantId, DeploymentType.Create)));
    }
}