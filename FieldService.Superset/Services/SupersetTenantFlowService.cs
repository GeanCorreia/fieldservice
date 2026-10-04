using FieldService.Shared.Configuration;
using FieldService.Superset.Configuration;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace FieldService.Superset.Services;

internal class SupersetTenantFlowService : ISupersetTenantFlowService
{
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly SupersetOptions _supersetOptions;
    private readonly ApplicationAccountOptions _applicationAccountOptions;
    private readonly ISupersetTenantInstanceProcessingLock _processingLock;
    private readonly ISupersetTenantFlowRepository _supersetTenantFlowRepository;
    private readonly ISupersetContainerRepository _supersetContainerRepository;
    
    public SupersetTenantFlowService(
        ISupersetTenantService supersetTenantService,
        IOptions<SupersetOptions> supersetOptions,
        IOptions<ApplicationAccountOptions> options,
        ISupersetTenantInstanceProcessingLock processingLock,
        ISupersetTenantFlowRepository supersetTenantFlowRepository,
        ISupersetContainerRepository supersetContainerRepository)
    {
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetOptions = supersetOptions?.Value ?? throw new ArgumentNullException(nameof(supersetOptions));
        _processingLock = processingLock ?? throw new ArgumentNullException(nameof(processingLock));
        _supersetTenantFlowRepository = supersetTenantFlowRepository ?? throw new ArgumentNullException(nameof(supersetTenantFlowRepository));
        _supersetContainerRepository = supersetContainerRepository ?? throw new ArgumentNullException(nameof(supersetContainerRepository));
        _applicationAccountOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    public async Task<SupersetTenant> BuildSupersetTenantCreationFlowAsync(
        Guid flowId, CancellationToken cancellationToken = default)
    {
        var flow = await _supersetTenantFlowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow == null)
        {
            throw new SupersetTenantFlowNotFoundException(flowId);
        }
        
        if (flow.Status != SupersetTenantDeployStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot build creation flow for flow with status '{flow.Status}'.");
        }

        Guid containerId = flow.CreatedContainerId ?? _supersetOptions.ApplicationSupersetContainerId;
        
        var container = await _supersetContainerRepository.GetSupersetContainerByIdAsync(containerId, cancellationToken);
        if (container == null)
        {
            throw new SupersetContainerNotFoundException(containerId);
        }

        bool isDedicated = container.ExecutionType == ExecutionType.AlwaysOn;
        bool isScheduled = container.ExecutionType == ExecutionType.Scheduled;
        var secretKeyId = flow.SecretKeyId ?? _supersetOptions.SecretKeyId;
        if(flow.ConnectionStringId == null)
        {
            throw new InvalidOperationException($"ConnectionStringId is null for flow '{flowId}'.");
        }
        
        return SupersetTenant.Create(
            tenantId: flow.TenantId,
            supersetContainer: container,
            flow.ConnectionStringId.Value
        );
    }

    public async Task<SupersetTenant> BuildSupersetTenantUpdateFlowAsync(
        Guid flowId, CancellationToken cancellationToken = default)
    {
        var flow = await _supersetTenantFlowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow == null)
        {
            throw new SupersetTenantFlowNotFoundException(flowId);
        }
        
        if (flow.Status != SupersetTenantDeployStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot build creation flow for flow with status '{flow.Status}'.");
        }
        
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(flow.TenantId, cancellationToken);
        
        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(flow.TenantId);
        }
        
        var container = await _supersetContainerRepository
            .GetSupersetContainerByIdAsync(supersetTenant.Container.Id, cancellationToken);
        
        if(container == null)
        {
            throw new SupersetContainerNotFoundException(supersetTenant.Container.Id);
        }
        
        supersetTenant.UpdateContainer(container);
        return supersetTenant;
    }
    
}