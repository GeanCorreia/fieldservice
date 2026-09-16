using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Utils;
using Humanizer;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries;

internal class GetGuestTokenHandler : IRequestHandler<GetGuestTokenQuery, SupersetTokenResponse>
{
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ILogger<GetGuestTokenHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetResourceService _supersetResourceService;
    
    public GetGuestTokenHandler(
        ILogger<GetGuestTokenHandler> logger, 
        IMediator mediator, 
        ISupersetApi supersetApi, 
        ISupersetResourceService supersetResourceService,
        ISupersetAuthService supersetAuthService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetResourceService = supersetResourceService ?? throw new ArgumentNullException(nameof(supersetResourceService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
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
        
        var resource = await _supersetResourceService.GetResourceBySupersetIdAsync(
            request.SupersetClientRequest.ResourceId,
            cancellationToken);

        if (resource == null)
        {
            throw new  KeyNotFoundException($"No resource found for Superset ID: {request.SupersetClientRequest.ResourceId}");
        }

        if (!resource.HasReadAccess(user))
        {
            throw new UnauthorizedAccessException($"User does not have read access to resource {resource.SupersetResourceId}");
        }
        
        var userName = SupersetUsernameResolver.Resolve(user);
        
        var resources = new List<SupersetResourcePayload>
        {
            new(resource.ResourceType.ToString().ToLowerInvariant(), resource.SupersetResourceId)
        };
        
        var rls = new List<SupersetRlsPayload>
        {
            new SupersetRlsPayload($"tenant_id = '{user.TenantDto.TenantId}'")
        };
        
        var guestTokenRequest = new SupersetGuestTokenRequest(
            userName,
            resources,
            rls
        );
        
        var adminToken = await _supersetAuthService.GetAdminToken();
        if (string.IsNullOrEmpty(adminToken))
        {
            throw new UnauthorizedAccessException("Failed to retrieve admin token for Superset");
        }
        var bearerToken = $"Bearer {adminToken}";

        SupersetGuestTokenResponse supersetApiResponse = await _supersetApi.GetGuestTokenAsync(
            bearerToken, 
            guestTokenRequest, 
            cancellationToken);
        
        return new SupersetTokenResponse(supersetApiResponse.token);
    }
}
