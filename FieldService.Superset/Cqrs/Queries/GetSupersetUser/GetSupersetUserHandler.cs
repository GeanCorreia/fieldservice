using FieldService.Shared.Types;
using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries.GetSupersetUser;

internal class GetSupersetUserHandler : IRequestHandler<GetSupersetUserQuery, SupersetUserDetailDto>
{
    private readonly ILogger<GetSupersetUserHandler> _logger;
    private readonly ISupersetService _supersetService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetUserManagement _supersetUserManagement;

    public GetSupersetUserHandler(
        ILogger<GetSupersetUserHandler> logger,
        ISupersetService supersetService,
        ISupersetAuthService supersetAuthService,
        ISupersetUserManagement supersetUserManagement)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetUserManagement = supersetUserManagement ?? throw new ArgumentNullException(nameof(supersetUserManagement));
    }

    public async Task<SupersetUserDetailDto> Handle(GetSupersetUserQuery request, CancellationToken cancellationToken)
    {
        var tenantConfig = await _supersetService.GetSupersetTenantByTenantIdAsync(request.TenantId, cancellationToken);
        if (tenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }

        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        var userName = SupersetUsernameResolver.ResolveUsername(request.UserId, request.TenantId);
        var filterQuery = $"(filters:!((col:username,opr:eq,value:'{userName}')))";
        var host = new Uri(tenantConfig.FqdnUrl);

        var response = await _supersetUserManagement.GetUsersAsync(
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