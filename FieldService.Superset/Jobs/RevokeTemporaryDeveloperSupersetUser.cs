using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using FieldService.Superset.Attributes;
using FieldService.Superset.Cqrs.Commands.DeleteUser;
using FieldService.Superset.Cqrs.Commands.UpdateUser;
using FieldService.Superset.Cqrs.Queries.GetSupersetUser;
using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Jobs;

internal record SupersetRevokeTemporaryDeveloperUserPayload(
    Guid UserId,
    Guid TenantId) : AbstractMessagePayload<SupersetRevokeTemporaryDeveloperUserPayload>;

internal record SupersetRevokeTemporaryDeveloperUserJob : Job<SupersetRevokeTemporaryDeveloperUserPayload>
{
    public static readonly JobType JobType = "superset-revoke-temporary-developer-user-job";
    
    internal SupersetRevokeTemporaryDeveloperUserJob(SupersetRevokeTemporaryDeveloperUserPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}

internal sealed class SupersetRevokeTemporaryDeveloperUserProducer : 
    AbstractPublishDelayedProducerWithRequest<SupersetRevokeTemporaryDeveloperUserConsumer, SupersetRevokeTemporaryDeveloperUserPayload>
{
    public SupersetRevokeTemporaryDeveloperUserProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string PublishDelayed(SupersetRevokeTemporaryDeveloperUserPayload payload, TimeSpan delay)
    {
        var job = new SupersetRevokeTemporaryDeveloperUserJob(payload);
        return PublishDelayed(job, delay);
    }
}

internal class SupersetRevokeTemporaryDeveloperUserConsumer : IQueueConsumer<SupersetRevokeTemporaryDeveloperUserPayload>
{
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ILogger<SupersetRevokeTemporaryDeveloperUserConsumer> _logger;
    private readonly IMediator _mediator;

    public SupersetRevokeTemporaryDeveloperUserConsumer(
        ISupersetTenantService supersetTenantService, 
        ILogger<SupersetRevokeTemporaryDeveloperUserConsumer> logger,
        IMediator mediator)
    {
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
        public async Task ExecuteAsync(
            Job<SupersetRevokeTemporaryDeveloperUserPayload> job, 
            CancellationToken ct = default)
        {
            var supersetUser = await _mediator.Send(
                new GetSupersetUserQuery(job.Payload.UserId, job.Payload.TenantId), 
                ct);
            
            if (supersetUser == null)
            {
                return;
            }
            
            if(!supersetUser.Permissions.Contains(SupersetPermissions.DevelopmentScopePermission))
            {
                return;
            }
            
            var isTenantUser =  supersetUser.Permissions.Contains(SupersetPermissions.ProductionScopePermission);
            
            if (!isTenantUser)
            {
                await _mediator.Send(new DeleteUserCommand(job.Payload.UserId, job.Payload.TenantId), ct);
                return;
            }
            
            supersetUser.Permissions.Remove(SupersetPermissions.DevelopmentScopePermission);
            
            var updateRequest = new SupersetUserUpdateRequest
            {
                Permissions = supersetUser.Permissions
            };
            
            await _mediator.Send(
                new UpdateUserCommand(job.Payload.UserId, job.Payload.TenantId, updateRequest), 
                ct);
        }
        
    
}

   