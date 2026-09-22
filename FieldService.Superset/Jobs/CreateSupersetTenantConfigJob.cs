using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Logs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Jobs;

internal record CreateSupersetTenantConfigPayload(
    SupersetTenantConfigParams ConfigParams) : AbstractMessagePayload<CreateSupersetTenantConfigPayload>;
internal record CreateSupersetTenantConfigJob : Job<CreateSupersetTenantConfigPayload>
{
    public static readonly JobType JobType = "superset-tenant-config-create-job";

    internal CreateSupersetTenantConfigJob(CreateSupersetTenantConfigPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.ConfigParams.TenantId))
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
    
    public CreateSupersetTenantConfigJobConsumer(
        ISupersetService supersetService, 
        ILogger<CreateSupersetTenantConfigJobConsumer> logger)
    {
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task ExecuteAsync(Job<CreateSupersetTenantConfigPayload> job, CancellationToken ct = default)
    {
        
        if (job.Context.Type != CreateSupersetTenantConfigJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
        var payload = job.Payload;
        
        var supersetTenantConfig = SupersetTenantConfig.Create(
            payload.ConfigParams);

        try
        {
            await _supersetService.SaveAsync(supersetTenantConfig, ct);
        }
        catch (Exception ex)
        {
            _logger.LogSupersetTenantConfigCreateError(
                LogLevel.Error,
                payload.ConfigParams.TenantId,
                payload.ConfigParams.ResourceId,
                payload.ConfigParams,
                ex.Message,
                ex);
            
            throw;
        }
    }
}