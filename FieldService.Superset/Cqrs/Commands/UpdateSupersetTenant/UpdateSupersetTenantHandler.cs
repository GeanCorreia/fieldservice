using FieldService.Storage.Interfaces;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;


namespace FieldService.Superset.Cqrs.Commands.UpdateSupersetTenant;

internal class UpdateSupersetTenantHandler : IRequestHandler<UpdateSupersetTenantCommand>
{
    private readonly ISupersetApi _supersetApi  ;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly IStorageService _storageService;
    private readonly ILogger<UpdateSupersetTenantHandler> _logger;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly UpdateSupersetTenantContainerJobProducer _updateSupersetTenantContainerJobProducer;
    public UpdateSupersetTenantHandler(
        ISupersetApi supersetApi, 
        ISupersetSecurityApi supersetSecurityApi, 
        IStorageService storageService,
        ILogger<UpdateSupersetTenantHandler> logger,
        ISupersetTenantService supersetTenantService,
        UpdateSupersetTenantContainerJobProducer updateSupersetTenantContainerJobProducer)
    {
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantService =
            supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _updateSupersetTenantContainerJobProducer = updateSupersetTenantContainerJobProducer ?? 
                                                    throw new ArgumentNullException(nameof(updateSupersetTenantContainerJobProducer));
    }

    public async Task Handle(
        UpdateSupersetTenantCommand request, 
        CancellationToken cancellationToken)
    {
        var payload = new UpdateSupersetTenantContainerJobPayload(request.TenantId, request.Configuration);
        var job = new UpdateSupersetTenantContainerJob(payload);
        _updateSupersetTenantContainerJobProducer
            .Publish(job);
        
    }
    
   
}