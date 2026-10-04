using FieldService.Data.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Hangfire;
using MediatR;

namespace FieldService.Superset.Jobs;

internal record SuspendSupersetTenantJobPayload(
    Guid TenantId) : AbstractMessagePayload<SuspendSupersetTenantJobPayload>;


internal record SuspendSupersetTenantJob : Job<SuspendSupersetTenantJobPayload>
{
    public static readonly JobType JobType = "superset-suspend-tenant-job";

    internal SuspendSupersetTenantJob(SuspendSupersetTenantJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal class SuspendSupersetTenantJobConsumer : IQueueConsumer<SuspendSupersetTenantJobPayload>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetTenantInstanceLifecycleService _supersetTenantInstanceLifecycleService;
    private readonly ISupersetTenantService _supersetTenantService;

    public SuspendSupersetTenantJobConsumer(
        IUnitOfWork unitOfWork,
        ISupersetTenantInstanceLifecycleService supersetTenantInstanceLifecycleService, 
        ISupersetTenantService supersetTenantService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _supersetTenantInstanceLifecycleService = supersetTenantInstanceLifecycleService ?? throw new ArgumentNullException(nameof(supersetTenantInstanceLifecycleService));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
    }

    public async Task ExecuteAsync(Job<SuspendSupersetTenantJobPayload> job, CancellationToken cancellationToken = default)
    {
        if (job.Context.Type != SuspendSupersetTenantJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
        
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(job.Payload.TenantId, cancellationToken);
        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(job.Payload.TenantId);
        }
        
        
        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            supersetTenant.MarkAsSuspended();
            await _supersetTenantService.SaveAsync(supersetTenant, cancellationToken);
            
            if (supersetTenant.Container.ExecutionType != ExecutionType.OnDemand)
            {
                await _supersetTenantInstanceLifecycleService.ScaleDownToZeroAsync(job.Payload.TenantId, cancellationToken);
            }
            
            await _unitOfWork.CommitAsync(cancellationToken);
        }   
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        
    }
    
}

internal class SuspendSupersetTenantJobProducer :AbstractPublishProducerWithRequest<SuspendSupersetTenantJobConsumer,SuspendSupersetTenantJobPayload>
{
    public SuspendSupersetTenantJobProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
}

