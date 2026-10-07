using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Configuration;
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
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Cqrs.Queries;

internal class GetGuestTokenHandler : IRequestHandler<GetGuestTokenQuery, GuestTokenResponse>
{
    private readonly ILogger<GetGuestTokenHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly StartSupersetTenantInstanceCreatedJobProducerWithRequest _startSupersetTenantInstanceCreatedJobProducerWithRequest;
    private readonly SupersetOptions _supersetOptions;
    
    public GetGuestTokenHandler(
        ILogger<GetGuestTokenHandler> logger, 
        IMediator mediator, 
        ISupersetApi supersetApi, 
        ISupersetAuthService supersetAuthService,
        ISupersetTenantService supersetTenantService,
        StartSupersetTenantInstanceCreatedJobProducerWithRequest startSupersetTenantInstanceCreatedJobProducerWithRequest,
        ISupersetSecurityApi supersetSecurityApi,
        IOptions<SupersetOptions> supersetOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetOptions = supersetOptions?.Value ?? throw new ArgumentNullException(nameof(supersetOptions));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
        _startSupersetTenantInstanceCreatedJobProducerWithRequest = startSupersetTenantInstanceCreatedJobProducerWithRequest ?? 
                                                          throw new ArgumentNullException(
                                                              nameof(startSupersetTenantInstanceCreatedJobProducerWithRequest));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
    }
    
    
    public async Task<GuestTokenResponse> Handle(
        GetGuestTokenQuery request, 
        CancellationToken cancellationToken)
    {
        if (!request.Resources.Any())
        {
            throw new InvalidOperationException("Resources cannot be empty");
        }
        var user = await _mediator.Send(new GetUserTenantQuery(request.UserId, request.TenantId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }
        
        var tenantId = request.IsApplicationResource ? _supersetOptions.ApplicationSupersetTenantId : request.TenantId; 

        var rls = new List<SupersetRlsApiPayload>();
        
        if(_supersetOptions.ApplicationSupersetTenantId == request.TenantId)
        {
            rls.Add(SupersetRlsApiPayload.ForTenant(request.TenantId));
        }
        
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(
            tenantId, 
            cancellationToken);
        
        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }
   
        var userName = SupersetUsernameResolver.ResolveUsername(user);
        
        var resources = request.Resources.Select(r => new SupersetResourceApiPayload(
            r.Type.ToString().ToLowerInvariant(),
            r.Id)).ToList();
        
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

        SupersetGuestTokenApiResponse supersetApiResponse = await _supersetSecurityApi.GetGuestTokenAsync(
            new Uri(supersetTenant.FqdnUrl),
            bearerToken, 
            guestTokenRequest, 
            cancellationToken);
        
        return new GuestTokenResponse(supersetApiResponse.Token, DateTime.UtcNow.AddMinutes(5));
        
        
    }
    
    
}