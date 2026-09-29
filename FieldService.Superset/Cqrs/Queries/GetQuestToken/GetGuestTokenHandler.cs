using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using FieldService.Superset.Utils;
using Humanizer;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries;

internal class GetGuestTokenHandler : IRequestHandler<GetGuestTokenQuery, GuestTokenResponse>
{
    private readonly ILogger<GetGuestTokenHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetService _supersetService;
    private readonly ISupersetAuthService _supersetAuthService;
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
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _startSupersetTenantInstanceCreatedJobProducer = startSupersetTenantInstanceCreatedJobProducer ?? 
                                                          throw new ArgumentNullException(
                                                              nameof(startSupersetTenantInstanceCreatedJobProducer));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
    }

    public async Task<GuestTokenResponse> Handle(
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
        
        var userName = SupersetUsernameResolver.ResolveUsername(user);
        
        var resources = new List<SupersetResourceApiPayload>
        {
            new(request.SupersetResourceDto.ResourceType.ToString().ToLowerInvariant(), request.SupersetResourceDto.ResourceId)
        };

        var rls = new List<SupersetRlsApiPayload>();
       
        var guestTokenRequest = new SupersetGuestTokenApiRequest(
            User: new SupersetGuestTokenUserApiPayload(userName),
            Resources: resources,
            Rls: rls
        );
        
        var adminToken = await _supersetAuthService.GetAdminToken(user.TenantDto.TenantId, cancellationToken);
        
        if (string.IsNullOrEmpty(adminToken))
        {
            throw new UnauthorizedAccessException("Failed to retrieve admin token for Superset");
        }
        
        var bearerToken = $"Bearer {adminToken}";

        SupersetGuestTokenApiResponse supersetApiResponse = await _supersetApi.GetGuestTokenAsync(
            new Uri(supersetInstance.TenantConfig.FqdnUrl),
            bearerToken, 
            guestTokenRequest, 
            cancellationToken);
        
        return new GuestTokenResponse(supersetApiResponse.Token, DateTime.UtcNow.AddMinutes(5));
    }
}