using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Refit;

namespace FieldService.Superset.Services;

internal class SupersetRoleServices : ISupersetRoleServices
{
    private const string DatabaseAccessPermission = "database_access";
    private const string SchemaAccessPermission = "schema_access";

    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetTenantService _supersetTenantService;

    public SupersetRoleServices(
        ISupersetSecurityApi supersetSecurityApi,
        ISupersetAuthService supersetAuthService,
        ISupersetTenantService supersetTenantService)
    {
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
    }
    
    public async Task<IEnumerable<SupersetRole>> GetSupersetRoles(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var context = await CreateContextAsync(tenantId, cancellationToken);

        var rolesResponse = await _supersetSecurityApi.GetRolesAsync(
            context.Host,
            context.BearerToken,
            cancellationToken);

        var roleDetailTasks = rolesResponse.Result.Select(role =>
            _supersetSecurityApi.GetRoleByIdAsync(context.Host, context.BearerToken, role.Id, cancellationToken));

        var roleDetails = await Task.WhenAll(roleDetailTasks);

        return roleDetails.Select(detail =>
            new SupersetRole(
                detail.Id,
                detail.Name,
                detail.Permissions
                    .Select(permission => BuildPermissionKey(permission.EffectivePermissionName, permission.EffectiveViewMenuName))
                    .Where(permission => !string.IsNullOrWhiteSpace(permission))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()));
    }
    
    private async Task<SupersetRoleContext> CreateContextAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _supersetTenantService.GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Superset instance for tenant '{tenantId}' not found.");
        }
        
        await _supersetTenantService.EnsureSupersetContainerActiveAsync(tenantId, cancellationToken);

        var adminToken = await _supersetAuthService.GetAdminToken(tenantId, cancellationToken);
        var expectedDatabaseNames = new[]
            {
                SupersetTenant.Database(tenantId).ToLowerInvariant(),
                $"db_tenant_{tenantId:N}".ToLowerInvariant()
            }
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new SupersetRoleContext(
            new Uri(tenant.Container.FqdnUrl),
            $"Bearer {adminToken}",
            expectedDatabaseNames);
    }
    
    private static string BuildPermissionKey(string? permissionName, string? viewMenuName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(viewMenuName)
            ? permissionName
            : $"{permissionName}:{viewMenuName}";
    }

    private sealed record SupersetRoleContext(
        Uri Host,
        string BearerToken,
        List<string> ExpectedDatabaseNames);
}

