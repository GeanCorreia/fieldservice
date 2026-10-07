using FieldService.Data.Interfaces;
using FieldService.Queue.Attributes;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Configuration;
using FieldService.Shared.Message;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantSecretKeyJobPayload(
    Guid CreationId,
    Guid TenantId) : AbstractMessagePayload<CreateSupersetTenantSecretKeyJobPayload>;

internal record CreateSupersetTenantSecretKeyJob : Job<CreateSupersetTenantSecretKeyJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-secret-key-create-job";

    internal CreateSupersetTenantSecretKeyJob(CreateSupersetTenantSecretKeyJobPayload payload)
        : base(payload, new JobContext(
                JobType,
                payload.TenantId,
                SupersetContainerDeploymentFlow.SupersetContainerDeploymentFlowContext(payload.CreationId)
            )
        )
    {
    }
}
[DisableRetry]
internal sealed class CreateSupersetTenantSecretKeyJobConsumer : IQueueConsumer<CreateSupersetTenantSecretKeyJobPayload>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetTenantFlowRepository _tenantFlowRepository;
    private readonly ISupersetSecretService _supersetSecretService;
    private readonly ApplicationAccountOptions _applicationAccountOptions;

    public CreateSupersetTenantSecretKeyJobConsumer(
        IUnitOfWork unitOfWork,
        ISupersetTenantFlowRepository tenantFlowRepository,
        ISupersetSecretService supersetSecretService,
        IOptions<ApplicationAccountOptions> applicationAccountOptions)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantFlowRepository = tenantFlowRepository ?? throw new ArgumentNullException(nameof(tenantFlowRepository));
        _supersetSecretService = supersetSecretService ?? throw new ArgumentNullException(nameof(supersetSecretService));
        _applicationAccountOptions = (applicationAccountOptions ?? throw new ArgumentNullException(nameof(applicationAccountOptions))).Value;
    }

    public async Task ExecuteAsync(Job<CreateSupersetTenantSecretKeyJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != CreateSupersetTenantSecretKeyJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var flow = await _tenantFlowRepository.GetByIdAsync(job.Payload.CreationId, ct);
        if (flow is null)
            throw new InvalidOperationException($"Superset tenant creation with ID {job.Payload.CreationId} not found.");

        if (flow.TenantId != job.Payload.TenantId)
            throw new InvalidOperationException($"Tenant ID mismatch for creation process. Expected: {flow.TenantId}, Actual: {job.Payload.TenantId}.");

        if (flow.Status == SupersetTenantDeployStatus.Cancelled)
            throw new InvalidOperationException($"Superset tenant creation process for tenant {job.Payload.TenantId} has been cancelled.");

        if (flow.SecretKeyId.HasValue)
            return;

        var (secretKeyId, _) = await _supersetSecretService.CreateSupersetSecretApiKey(
            job.Payload.TenantId,
            _applicationAccountOptions.ServiceUserId,
            ct);

        flow.MarkSecretKeyCreated(secretKeyId);

        await _unitOfWork.BeginAsync(ct);
        try
        {
            await _tenantFlowRepository.SaveAsync(flow, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}

internal sealed class CreateSupersetTenantSecretKeyJobProducer
    : AbstractPublishProducerWithRequest<CreateSupersetTenantSecretKeyJobConsumer, CreateSupersetTenantSecretKeyJobPayload>
{
    public CreateSupersetTenantSecretKeyJobProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}
