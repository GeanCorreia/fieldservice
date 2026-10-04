using FieldService.Shared.Types;
using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries.GetSupersetUser;

internal class GetSupersetUserHandler : IRequestHandler<GetSupersetUserQuery, SupersetUserDetailDto>
{
    private readonly ILogger<GetSupersetUserHandler> _logger;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetSecurityApi _supersetSecurityApi;

    public GetSupersetUserHandler(
        ILogger<GetSupersetUserHandler> logger,
        ISupersetTenantService supersetTenantService,
        ISupersetAuthService supersetAuthService,
        ISupersetSecurityApi supersetSecurityApi)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
    }

    public async Task<SupersetUserDetailDto> Handle(GetSupersetUserQuery request, CancellationToken cancellationToken)
    {
        var tenantSuperset = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
        if (tenantSuperset == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        await _supersetTenantService.EnsureSupersetContainerActiveAsync(request.TenantId, cancellationToken);
        
        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        var userName = SupersetUsernameResolver.ResolveUsername(request.UserId, request.TenantId);
        var filterQuery = $"(filters:!((col:username,opr:eq,value:'{userName}')))";
        var host = new Uri(tenantSuperset.FqdnUrl);

        var response = await _supersetSecurityApi.GetUsersAsync(
            host,
            bearerToken,
            filterQuery,
            cancellationToken);

        var supersetUser = response.Result.FirstOrDefault();
        if (supersetUser == null)
        {
            return null;
        }


        var roleNames = supersetUser.Roles?
            .Select(role => role.Name)
            .Where(static roleName => !string.IsNullOrWhiteSpace(roleName))
            .ToList() ?? new List<string>();


        List<Permission> permissions = SupersetPermissions.MapDomainPermissions(roleNames);

        return new SupersetUserDetailDto(
            supersetUser.Id,
            supersetUser.Username,
            supersetUser.Email,
            supersetUser.FirstName,
            supersetUser.LastName,
            supersetUser.Active,
            permissions);
    }

    
}