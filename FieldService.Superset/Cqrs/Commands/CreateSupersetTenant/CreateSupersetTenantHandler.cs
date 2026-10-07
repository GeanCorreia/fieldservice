using System.Data.Common;
using Dapper;
using FieldService.Data.Interfaces;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKeyById;
using FieldService.SecretKey.Entities;
using FieldService.Superset.Broker;
using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FieldService.Superset.Cqrs.Commands.CreateSupersetTenant;

internal class CreateSupersetTenantHandler : IRequestHandler<CreateSupersetTenantCommand>
{

    private readonly IMediator _mediator;
    private readonly ISupersetRoleServices _supersetRoleServices;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupersetDataBaseService _supersetDataBaseService;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly ILogger<CreateSupersetTenantHandler> _logger;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetSecretService _supersetSecretService;
    private readonly SupersetOptions _supersetOptions;
    private readonly CreateSupersetTenantContainerJobProducer _createSupersetTenantContainerJobProducer;
    private readonly SupersetTenantDeploymentBrokerProducer _supersetTenantDeploymentBrokerProducer;

    public CreateSupersetTenantHandler(
        IMediator mediator,
        ISupersetRoleServices supersetRoleServices,
        IUnitOfWork unitOfWork,
        ISupersetDataBaseService supersetDataBaseService, 
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ILogger<CreateSupersetTenantHandler> logger, ISupersetAuthService supersetAuthService,
        ISupersetTenantService supersetTenantService, ISupersetSecretService supersetSecretService,
        IOptions<SupersetOptions> supersetOptions,
        CreateSupersetTenantContainerJobProducer createSupersetTenantContainerJobProducer,
        SupersetTenantDeploymentBrokerProducer supersetTenantDeploymentBrokerProducer)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetRoleServices = supersetRoleServices ?? throw new ArgumentNullException(nameof(supersetRoleServices));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _supersetDataBaseService = supersetDataBaseService ?? throw new ArgumentNullException(nameof(supersetDataBaseService));
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetTenantService =
            supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetSecretService =
            supersetSecretService ?? throw new ArgumentNullException(nameof(supersetSecretService));
        _supersetOptions = supersetOptions?.Value ?? throw new ArgumentNullException(nameof(supersetOptions));
        _createSupersetTenantContainerJobProducer = createSupersetTenantContainerJobProducer ??
                                                               throw new ArgumentNullException(
                                                                   nameof(
                                                                       createSupersetTenantContainerJobProducer));
        _supersetTenantDeploymentBrokerProducer = supersetTenantDeploymentBrokerProducer ??
                                                  throw new ArgumentNullException(
                                                      nameof(supersetTenantDeploymentBrokerProducer));
    }


    public async Task Handle(
        CreateSupersetTenantCommand request,
        CancellationToken cancellationToken)
    {
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
        if (supersetTenant != null)
        {
            throw new InvalidOperationException($"Tenant with ID {request.TenantId} already exists.");
        }
        
        var flow = SupersetContainerDeploymentFlow.Create(request.Configuration, request.TenantId, request.DedicatedHostConnectionStringId);
        var payload = new CreateSupersetTenantContainerJobPayload(flow.Id, request.TenantId);
        var job = new CreateSupersetTenantContainerJob(payload);
        _createSupersetTenantContainerJobProducer.Publish(job);



    }


}