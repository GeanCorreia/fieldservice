using Azure;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Configuration;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Jobs;

internal record UpdateSupersetTenantContainerJobPayload(
    Guid TenantId,
    ContainerConfiguration Configuration) :
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
    private readonly IAzureContainerConfigurationService _containerConfigurationService;
    private readonly IContainerRepository _supersetContainerRepository;
    private readonly SupersetOptions _options;
    
    
    public UpdateSupersetTenantContainerJobHandler(
        ISupersetTenantInstanceProcessingLock supersetTenantInstanceProcessingLock,
        IAzureContainerConfigurationService containerConfigurationService,
        IContainerRepository supersetContainerRepository,
        IOptions<SupersetOptions> options)
    {
        _supersetTenantInstanceProcessingLock = supersetTenantInstanceProcessingLock ?? throw new ArgumentNullException(nameof(supersetTenantInstanceProcessingLock));
        _containerConfigurationService = containerConfigurationService ?? throw new ArgumentNullException(nameof(containerConfigurationService));
        _supersetContainerRepository = supersetContainerRepository ?? throw new ArgumentNullException(nameof(supersetContainerRepository));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        
    }

    public async Task ExecuteAsync(Job<UpdateSupersetTenantContainerJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != UpdateSupersetTenantContainerJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var tenantId = job.Payload.TenantId;
        var configuration = job.Payload.Configuration;

        var container = await _supersetContainerRepository.GetByTenantAndNameAsync(
            tenantId, SupersetTenant.ContainerResourceName(tenantId), ct);
        if (container == null)
            throw new SupersetContainerNotFoundException(tenantId);
        
        if (!await _supersetTenantInstanceProcessingLock.AcquireLock(tenantId, ct))
            throw new InvalidOperationException($"Failed to acquire lock for tenant '{tenantId}'.");

        try
        {
            if (string.IsNullOrWhiteSpace(container.ResourceId))
                throw new ResourceIdNotFoundException(container.ResourceId);

            await _containerConfigurationService.ApplyAsync(
                container.ResourceId,
                configuration,
                _options.Provisioning.KedaCronTimezone,
                ct);

            var currentConfiguration = container.Configuration;
            if (currentConfiguration.Equals(configuration))
                return;

            container.UpdateConfiguration(configuration);
            await _supersetContainerRepository.SaveAsync(container, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new ResourceIdNotFoundException(container.ResourceId);
        }
        finally
        {
            await _supersetTenantInstanceProcessingLock.ReleaseLock(tenantId, CancellationToken.None);
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
