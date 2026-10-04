using System.IdentityModel.Tokens.Jwt;
using System.Net;
using FieldService.Shared.Types;
using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Mappers;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Refit;

namespace FieldService.Superset.Services;

internal class SupersetAuthService : ISupersetAuthService
{
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ILogger<SupersetAuthService> _logger;
    private readonly SupersetLoginApiRequest _supersetLoginRequest;
    private readonly HybridCache _cache;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    
    private static readonly TimeSpan TokenExpirationBuffer = TimeSpan.FromSeconds(30);

    public static string AuthPrefix => "superset:auth:";

    public static string AdminAccessTokenCacheKey(Guid tenantId) => 
        $"{AuthPrefix}admin:tenant:{tenantId}:access";
    public static string AdminRefreshTokenCacheKey(Guid tenantId) => 
        $"{AuthPrefix}admin:tenant:{tenantId}:refresh";

    public static string UserAccessTokenCacheKey(Guid userId, Guid tenantId) => 
        $"{AuthPrefix}user:{userId}:{tenantId}:access";
    public static string UserRefreshTokenCacheKey(Guid userId, Guid tenantId) => 
        $"{AuthPrefix}user:{userId}:{tenantId}:refresh";

    public SupersetAuthService(
        ISupersetApi supersetApi,
        ISupersetTenantService supersetTenantService,
        ILogger<SupersetAuthService> logger,
        HybridCache cache,
        ISupersetSecurityApi supersetSecurityApi)
    {
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
    }

    public async Task<string> GetAdminToken(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (supersetTenant == null)
        {
            throw new KeyNotFoundException($"Superset instance for tenant '{tenantId}' not found or not running.");
        }
        
        await _supersetTenantService.EnsureSupersetContainerActiveAsync(tenantId, cancellationToken);

        var accessCacheKey = AdminAccessTokenCacheKey(tenantId);
        var refreshCacheKey = AdminRefreshTokenCacheKey(tenantId);

        return await GetOrRefreshTokenAsync(
            accessCacheKey,
            refreshCacheKey,
            async token => await _supersetSecurityApi.LoginAsync(new Uri(supersetTenant.Container.FqdnUrl), _supersetLoginRequest, token),
            supersetTenant.Container.FqdnUrl,
            cancellationToken);
    }

    public async Task<string> SupersetLogin(
        UserTenantDto user, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var tenantId = user.TenantDto.TenantId;
        var userId = user.Id;

        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (supersetTenant == null)
        {
            throw new KeyNotFoundException($"Superset instance for tenant '{tenantId}' not found or not running.");
        }

        var accessCacheKey = UserAccessTokenCacheKey(userId, tenantId);
        var refreshCacheKey = UserRefreshTokenCacheKey(userId, tenantId);

        var supersetLoginApiRequest = user.Map();

        return await GetOrRefreshTokenAsync(
            accessCacheKey,
            refreshCacheKey,
            async token => await _supersetSecurityApi.LoginAsync(new Uri(supersetTenant.Container.FqdnUrl), supersetLoginApiRequest, token),
            supersetTenant.Container.FqdnUrl,
            cancellationToken);
    }

    public async Task<SupersetRoleDto?> GetTenantScopeRole(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var tenantConfig = await _supersetTenantService.GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (tenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }
        var supersetRoles = await _supersetSecurityApi.GetRolesAsync(
            new Uri(tenantConfig.Container.FqdnUrl),
            $"Bearer {await GetAdminToken(tenantId, cancellationToken)}",
            cancellationToken);
        
        var tenantScopeRole = supersetRoles.Result
            .FirstOrDefault(role => role.Name.Equals(TenantScopeRole.RoleName(tenantId), StringComparison.OrdinalIgnoreCase));

        if (tenantScopeRole == null)
        {
            return null;
        }
        
        return new SupersetRoleDto(
            tenantScopeRole.Id,
            tenantScopeRole.Name);
    }

    public async Task CreateTenantScopeRole(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var tenantConfig = await _supersetTenantService.GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (tenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }

        var roleName = TenantScopeRole.RoleName(tenantId);
        var existingRole = await GetTenantScopeRole(tenantId, cancellationToken);
        if (existingRole != null)
        {
            return;
        }

        var adminToken = await GetAdminToken(tenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        var host = new Uri(tenantConfig.Container.FqdnUrl);

        var permissionResources = await _supersetSecurityApi.GetPermissionResourcesAsync(
            host,
            bearerToken,
            "(page:0,page_size:10000)",
            cancellationToken);

        var expectedDatabaseNames = new[]
            {
                $"db_tenant_{tenantId:N}".ToLowerInvariant(),
                SupersetTenant.Database(tenantId).ToLowerInvariant()
            }
            .Distinct()
            .ToList();

        var databaseAccessPermissionIds = permissionResources.Result
            .Where(permission =>
                string.Equals(permission.EffectivePermissionName, "database_access", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(permission.EffectiveViewMenuName) &&
                expectedDatabaseNames.Any(dbName =>
                    permission.EffectiveViewMenuName!.StartsWith($"[{dbName}]", StringComparison.OrdinalIgnoreCase)))
            .Select(permission => permission.Id)
            .Distinct()
            .ToList();

        if (!databaseAccessPermissionIds.Any())
        {
            throw new InvalidOperationException(
                $"Could not resolve 'database_access' permission for tenant '{tenantId}'. " +
                $"Expected database names: {string.Join(", ", expectedDatabaseNames)}.");
        }

        try
        {
            await _supersetSecurityApi.CreateRoleAsync(
                host,
                bearerToken,
                new SupersetCreateRoleApiRequest(roleName, databaseAccessPermissionIds),
                cancellationToken);
        }
        catch (ApiException ex) when (
            ex.StatusCode == HttpStatusCode.Conflict ||
            ex.StatusCode == HttpStatusCode.BadRequest ||
            (int)ex.StatusCode == 422)
        {
            var roleAfterConflict = await GetTenantScopeRole(tenantId, cancellationToken);
            if (roleAfterConflict != null)
            {
               return;
            }

            throw;
        }
    }

    private async Task<string> GetOrRefreshTokenAsync(
        string accessCacheKey,
        string refreshCacheKey,
        Func<CancellationToken, Task<SupersetLoginApiResponse>> loginFallbackFactory,
        string fqdnUrl,
        CancellationToken cancellationToken)
    {
        var cachedAccessToken = await _cache.GetOrCreateAsync<string?>(
            accessCacheKey, 
            _ => ValueTask.FromResult<string?>(null), 
            cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(cachedAccessToken))
        {
            return cachedAccessToken;
        }
        
        var refreshToken = await _cache.GetOrCreateAsync<string?>(
            refreshCacheKey, 
            _ => ValueTask.FromResult<string?>(null), 
            cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                var refreshResponse = await _supersetSecurityApi.RefreshTokenAsync(
                    new Uri(fqdnUrl),
                    $"Bearer {refreshToken}",
                    cancellationToken);
                
                var accessTtl = ExtractTokenTimeToLive(refreshResponse.AccessToken);
                if (accessTtl.HasValue)
                {
                    await _cache.SetAsync(
                        accessCacheKey, 
                        refreshResponse.AccessToken, 
                        new HybridCacheEntryOptions { Expiration = accessTtl.Value }, 
                        cancellationToken: cancellationToken);
                }
               

                return refreshResponse.AccessToken;
            }
            catch (Exception ex)
            {
                await _cache.RemoveAsync(refreshCacheKey, cancellationToken);
            }
        }
       
        var loginResponse = await loginFallbackFactory(cancellationToken);
        
        var newAccessTtl = ExtractTokenTimeToLive(loginResponse.AccessToken);
        if (newAccessTtl.HasValue)
        {
            await _cache.SetAsync(
                accessCacheKey, 
                loginResponse.AccessToken, 
                new HybridCacheEntryOptions { Expiration = newAccessTtl.Value }, 
                cancellationToken: cancellationToken);
        }
        
        
        if (!string.IsNullOrWhiteSpace(loginResponse.RefreshToken))
        {
            var refreshTtl = ExtractTokenTimeToLive(loginResponse.RefreshToken);
            if (refreshTtl.HasValue)
            {
                await _cache.SetAsync(
                    refreshCacheKey, 
                    loginResponse.RefreshToken, 
                    new HybridCacheEntryOptions { Expiration = refreshTtl.Value }, 
                    cancellationToken: cancellationToken);
            }
        }

        return loginResponse.AccessToken;
    }
    private TimeSpan? ExtractTokenTimeToLive(string jwtToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jwtToken) || !_jwtHandler.CanReadToken(jwtToken))
            {
                _logger.LogError("Invalid JWT format received. Unable to parse token for caching.");
                return null;
            }

            var token = _jwtHandler.ReadJwtToken(jwtToken);
            var expClaim = token.ValidTo; 

            if (expClaim == DateTimeOffset.MinValue)
            {
                _logger.LogError("JWT missing 'exp' claim. Cannot determine expiration time.");
                return null;
            }

            var timeRemaining = expClaim - DateTimeOffset.UtcNow - TokenExpirationBuffer;

            
            if (timeRemaining <= TimeSpan.Zero)
            {
                _logger.LogWarning("JWT is already expired or within the safety buffer window. Will not cache.");
                return null;
            }

            return timeRemaining;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JWT expiration claim. Token will NOT be cached.");
            return null;
        }
    }
    
}