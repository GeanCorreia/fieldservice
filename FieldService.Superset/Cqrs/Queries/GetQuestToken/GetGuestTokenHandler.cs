using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Broker;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using FieldService.Superset.Utils;
using Humanizer;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries;

internal class GetGuestTokenHandler : IRequestHandler<GetGuestTokenQuery, SupersetTokenResponse>
{
    private readonly ILogger<GetGuestTokenHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetService _supersetService;
    private readonly StartSupersetTenantInstanceCreatedJobProducer _startSupersetTenantInstanceCreatedJobProducer;
    
    public GetGuestTokenHandler(
        ILogger<GetGuestTokenHandler> logger, 
        IMediator mediator, 
        ISupersetApi supersetApi, 
        ISupersetAuthService supersetAuthService,
        ISupersetService supersetService,
        StartSupersetTenantInstanceCreatedJobProducer startSupersetTenantInstanceCreatedJobProducer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _startSupersetTenantInstanceCreatedJobProducer = startSupersetTenantInstanceCreatedJobProducer ?? 
                                                          throw new ArgumentNullException(
                                                              nameof(startSupersetTenantInstanceCreatedJobProducer));
        
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
    }
    public async Task<SupersetTokenResponse> Handle(
        GetGuestTokenQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(
            new GetUserTenantQuery(request.UserId, request.TenantId), 
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }
        
        if(! await _supersetService.HasSuperset(user.TenantDto.TenantId, cancellationToken))
        {
            throw new SupersetTenantNotFoundException(user.TenantDto.TenantId);
        }
        
        var supersetInstance = await _supersetService.GetSupersetTenantInstance(
            user.TenantDto.TenantId, 
            SupersetInstanceStatus.Running, 
            cancellationToken);
        
        

        if (supersetInstance == null)
        {
            _startSupersetTenantInstanceCreatedJobProducer.Publish(
                new StartSupersetTenantInstanceJob(user.TenantDto.TenantId));
            
            throw new SupersetTenantInstanceNotRunningException(user.TenantDto.TenantId);
        }
        
        var userName = SupersetUsernameResolver.Resolve(user);
        
        var resources = new List<SupersetResourcePayload>
        {
            new(request.SupersetResourceDto.ResourceType.ToString(), request.SupersetResourceDto.ResourceId)
        };

        var rls = new List<SupersetRlsPayload>();
       
        
        var guestTokenRequest = new SupersetGuestTokenRequest(
            userName,
            resources,
            rls
        );
        
        var adminToken = await _supersetService.GetAdminToken(user.TenantDto.TenantId, cancellationToken);
        
        if (string.IsNullOrEmpty(adminToken))
        {
            throw new UnauthorizedAccessException("Failed to retrieve admin token for Superset");
        }
        
        var bearerToken = $"Bearer {adminToken}";

        SupersetGuestTokenResponse supersetApiResponse = await _supersetApi.GetGuestTokenAsync(
            new Uri(supersetInstance.TenantConfig.FqdnUrl),
            bearerToken, 
            guestTokenRequest, 
            cancellationToken);
        
        return new SupersetTokenResponse(supersetApiResponse.token);
    }
}
