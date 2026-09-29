using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries.GetTenantSupersetUsers;

internal class GetTenantSupersetUsersHandler : IRequestHandler<GetTenantSupersetUsersQuery, SupersetUserListResponseDto>
{
    private readonly ILogger<GetTenantSupersetUsersHandler> _logger;
    private readonly ISupersetService _supersetService;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetAuthService _supersetAuthService;

    public GetTenantSupersetUsersHandler(ILogger<GetTenantSupersetUsersHandler> logger,
        ISupersetService supersetService, IMediator mediator, ISupersetApi supersetApi,
        ISupersetAuthService supersetAuthService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
    }

    public async Task<SupersetUserListResponseDto> Handle(
        GetTenantSupersetUsersQuery request, 
        CancellationToken cancellationToken)
    {
        var supersetTenantConfig = await _supersetService
            .GetSupersetTenantByTenantIdAsync(request.TenantId, cancellationToken);

        if (supersetTenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        
        var fqdnUrl = new Uri(supersetTenantConfig.FqdnUrl);
        
        var supersetUsers = await _supersetApi
            .GetTenantSupersetUsersAsync(fqdnUrl, bearerToken, cancellationToken);
        
        return new SupersetUserListResponseDto(
            request.TenantId,
            supersetUsers.Result.Select(user => new SupersetUserDetailResponseDto(
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Active,
                SupersetPermissions.MapDomainPermissions(
                    user.Roles?.Select(role => role.Name)
                )
            )).ToList()
        );
    }
}
     