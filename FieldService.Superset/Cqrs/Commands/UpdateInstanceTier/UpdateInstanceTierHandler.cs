using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using FieldService.Superset.Jobs;

namespace FieldService.Superset.Cqrs.Commands.UpdateInstanceTier;

internal class UpdateInstanceTierHandler : IRequestHandler<UpdateInstanceTierCommand>
{
    private readonly ISupersetApi _supersetApi  ;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly IStorageService _storageService;
    private readonly ILogger<UpdateInstanceTierHandler> _logger;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly CreateSupersetTenantContainerJobProducer _createSupersetTenantContainerJobProducer;

    public UpdateInstanceTierHandler(
        ISupersetApi supersetApi, 
        ISupersetSecurityApi supersetSecurityApi, 
        IStorageService storageService,
        ILogger<UpdateInstanceTierHandler> logger,
        ISupersetTenantService supersetTenantService,
        CreateSupersetTenantContainerJobProducer createSupersetTenantContainerJobProducer)
    {
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantService =
            supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _createSupersetTenantContainerJobProducer = createSupersetTenantContainerJobProducer ??
                                                    throw new ArgumentNullException(
                                                        nameof(createSupersetTenantContainerJobProducer));
    }

    public async Task Handle(
        UpdateInstanceTierCommand request, 
        CancellationToken cancellationToken)
    {
        // var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
        //
        // if (supersetTenant == null)
        // {
        //     throw new SupersetTenantNotFoundException(request.TenantId);
        // }
        //
        // if (supersetTenant.Container.ExecutionType == request.CreateParams.ExecutionType)
        // {
        //     if(request.CreateParams.ExecutionType == ExecutionType.OnDemand)
        //     {
        //         throw new InvalidOperationException("Updating instance tier to OnDemand is not allowed. " +
        //                                             "It's already OnDemand.");
        //     }  
        //     
        //     if(request.CreateParams.ExecutionType == ExecutionType.AlwaysOn)
        //     {
        //         throw new InvalidOperationException("Updating instance tier to AlwaysOn is not allowed. " +
        //                                             "It's already AlwaysOn.");
        //     }
        //
        //     if (request.CreateParams.ExecutionType == ExecutionType.Scheduled &&
        //         request.CreateParams.ScheduledExecutionWindow == supersetTenant.Container.ExecutionWindow)
        //     {
        //         throw new InvalidOperationException("Updating instance tier to Scheduled is not allowed. " +
        //                                             "It's already Scheduled with the same execution window.");
        //     }
        // }
        
        preciso criar um job
        
    }
    
   
}