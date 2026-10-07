using FieldService.Data.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Hangfire;

namespace FieldService.Superset.Jobs;

internal record UpdateSupersetTenantContainerJobPayload(
    Guid TenantId,
    SupersetContainerConfiguration Configuration) :
    AbstractMessagePayload<UpdateSupersetTenantContainerJobPayload>;

internal record UpdateSupersetTenantContainerJob : Job<UpdateSupersetTenantContainerJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-container-update-job";

    internal UpdateSupersetTenantContainerJob(UpdateSupersetTenantContainerJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal class UpdateSupersetTenantContainerJobHandler : IQueueConsumer<UpdateSupersetTenantContainerJobPayload>
{
    private readonly ISupersetTenantInstanceProcessingLock _supersetTenantInstanceProcessingLock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetContainerConfigurationService _supersetContainerConfigurationService;
    private readonly ISupersetContainerRepository _supersetContainerRepository;
    
    
    public UpdateSupersetTenantContainerJobHandler(
        ISupersetTenantInstanceProcessingLock supersetTenantInstanceProcessingLock,
        IUnitOfWork unitOfWork,
        ISupersetContainerConfigurationService supersetContainerConfigurationService,
        ISupersetContainerRepository supersetContainerRepository)
    {
        _supersetTenantInstanceProcessingLock = supersetTenantInstanceProcessingLock ?? throw new ArgumentNullException(nameof(supersetTenantInstanceProcessingLock));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _supersetContainerConfigurationService = supersetContainerConfigurationService ?? throw new ArgumentNullException(nameof(supersetContainerConfigurationService));
        _supersetContainerRepository = supersetContainerRepository ?? throw new ArgumentNullException(nameof(supersetContainerRepository));
        
    }

    public async Task ExecuteAsync(Job<UpdateSupersetTenantContainerJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != UpdateSupersetTenantContainerJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var tenantId = job.Payload.TenantId;
        var configuration = job.Payload.Configuration;

        var container = await _supersetContainerRepository.GetSupersetContainerByTenantIdAsync(tenantId, ct);
        if (container == null)
            throw new SupersetContainerNotFoundException(tenantId);
        
        if (!await _supersetTenantInstanceProcessingLock.AcquireLock(tenantId, ct))
            throw new InvalidOperationException($"Failed to acquire lock for tenant '{tenantId}'.");

        try
        {
            await _supersetContainerConfigurationService.ApplyContainerConfigurationAsync(
                container.ResourceId,
                configuration,
                ct);

            var currentConfiguration = container.Configuration;
            if (currentConfiguration.Equals(configuration))
                return;

            container.UpdateConfiguration(configuration);
            await _supersetContainerRepository.SaveSupersetContainerAsync(container, ct);
        }
        catch 
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
        finally
        {
            await _supersetTenantInstanceProcessingLock.ReleaseLock(tenantId, ct);
        }


    }
}

internal class UpdateSupersetTenantContainerJobProducer : AbstractPublishProducerWithRequest<UpdateSupersetTenantContainerJobHandler, UpdateSupersetTenantContainerJobPayload>
{
    public UpdateSupersetTenantContainerJobProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}
