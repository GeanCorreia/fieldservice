using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Configuration;
using FieldService.Superset.Cqrs.Queries.GetTenantSupersetUsers;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Mappers;
using FieldService.Superset.Utils;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Services;

internal class SupersetResourceServices : ISupersetResourceServices
{
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ILogger<SupersetResourceServices> _logger;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly SupersetOptions _supersetOptions;
    private readonly HybridCache _cache;
    private readonly IMediator _mediator;

    public SupersetResourceServices(
        ILogger<SupersetResourceServices> logger,
        ISupersetApi supersetApi,
        ISupersetAuthService supersetAuthService,
        ISupersetTenantService supersetTenantService,
        IOptions<SupersetOptions> supersetOptions,
        HybridCache cache,
        IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetOptions = supersetOptions?.Value ?? throw new ArgumentNullException(nameof(supersetOptions));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }
    private static string GetTenantSharedResourcesCacheKey(Guid tenantId) 
        => $"superset:resources:tenant:{tenantId}:shared";

    private static string GetUserSavedQueriesCacheKey(Guid tenantId, Guid userId) 
        => $"superset:resources:tenant:{tenantId}:user:{userId}:queries";
    
    public async Task<SupersetTenantResources> GetSupersetTenantResourcesAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        var sharedResourcesTask = GetSharedTenantResourcesAsync(tenantId, cancellationToken);
        var userQueriesTask = GetSavedQueriesAsync(userId, tenantId, cancellationToken);
        await Task.WhenAll(sharedResourcesTask, userQueriesTask);

        var (dashboards, charts, datasets) = await sharedResourcesTask;
        var savedQueries = await userQueriesTask;

        return new SupersetTenantResources(dashboards, charts, datasets, savedQueries);
    }

    public async Task<SupersetTenantResources> GetSupersetTenantResourcesAdminAsync(
        Guid TenantId, 
        CancellationToken cancellationToken)
    {
        var sharedResources = await GetSharedTenantResourcesAsync(TenantId, cancellationToken);
        var savedQueries = new List<SupersetSavedQueryDto>();
        
        
        var users = await _mediator.Send(new GetTenantSupersetUsersQuery(TenantId, cancellationToken));

        if (users != null)
        {
            var domainUserIds = users.Users.Select(u => SupersetUsernameResolver.ResolveDomainUserId(u.Username)).ToList();
            var userQueriesTasks = domainUserIds.Select(userId => GetSavedQueriesAsync(userId: userId, tenantId: TenantId, cancellationToken));
            var userQueriesResults = await Task.WhenAll(userQueriesTasks);
            savedQueries = userQueriesResults.SelectMany(q => q).ToList();
        }
        
        return new SupersetTenantResources(sharedResources.Dashboards, sharedResources.Charts, sharedResources.Datasets, savedQueries);
        
        
    }

    public async Task<IEnumerable<SupersetDashboardDto>> GetDashboardsAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        var (dashboards, _, _) = await GetSharedTenantResourcesAsync(tenantId, cancellationToken);
        return dashboards;
    }

    public async Task<IEnumerable<SupersetChartDto>> GetChartsAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        var (_, charts, _) = await GetSharedTenantResourcesAsync(tenantId, cancellationToken);
        return charts;
    }

    public async Task<IEnumerable<SupersetDatasetDto>> GetDatasetsAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        var (_, _, datasets) = await GetSharedTenantResourcesAsync(tenantId, cancellationToken);
        return datasets;
    }

    public async Task<IEnumerable<SupersetSavedQueryDto>> GetSavedQueriesAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        var cacheKey = GetUserSavedQueriesCacheKey(tenantId, userId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await FetchUserSavedQueriesFromApiAsync(userId, tenantId, ct),
            options: GetCacheEntryOptions(),
            cancellationToken: cancellationToken
        );
    }
    
    public async Task UpdateSupersetTenantResourcesAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {

        var sharedKey = GetTenantSharedResourcesCacheKey(tenantId);
        var userQueriesKey = GetUserSavedQueriesCacheKey(tenantId, userId);
        
        await _cache.RemoveAsync(sharedKey, cancellationToken);
        await _cache.RemoveAsync(userQueriesKey, cancellationToken);
    }
    
    private HybridCacheEntryOptions GetCacheEntryOptions() => new()
    {
        Expiration = TimeSpan.FromMinutes(_supersetOptions.ResourceCacheTtlMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(_supersetOptions.ResourceCacheTtlMinutes)
    };
    
    private async Task<(IEnumerable<SupersetDashboardDto> Dashboards, IEnumerable<SupersetChartDto> Charts, IEnumerable<SupersetDatasetDto> Datasets)> 
        GetSharedTenantResourcesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cacheKey = GetTenantSharedResourcesCacheKey(tenantId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await FetchSharedTenantResourcesFromApiAsync(tenantId, ct),
            options: GetCacheEntryOptions(),
            cancellationToken: cancellationToken
        );
    }
    
    private async Task<(IEnumerable<SupersetDashboardDto> Dashboards, IEnumerable<SupersetChartDto> Charts, IEnumerable<SupersetDatasetDto> Datasets)> 
        FetchSharedTenantResourcesFromApiAsync(Guid tenantId, CancellationToken cancellationToken)
    {

        var supersetTenant = await _supersetTenantService
            .GetSupersetTenantByIdAsync(tenantId, cancellationToken);

        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }
        
        await _supersetTenantService.EnsureSupersetContainerActiveAsync(tenantId, cancellationToken);

        var adminToken = await _supersetAuthService.GetAdminToken(tenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        var fqdnUrl = new Uri(supersetTenant.Container.FqdnUrl);

        var tenantScopeRole = await _supersetAuthService.GetTenantScopeRole(tenantId, cancellationToken);
        if (tenantScopeRole == null)
        {
            return (Enumerable.Empty<SupersetDashboardDto>(), Enumerable.Empty<SupersetChartDto>(), Enumerable.Empty<SupersetDatasetDto>());
        }

        

        var dashboardsTask = FetchSafelyAsync(() => _supersetApi.GetDashboardsAsync(
            fqdnUrl, 
            bearerToken, 
            ct: cancellationToken));

        var chartsTask = FetchSafelyAsync(() => _supersetApi.GetChartsAsync(
            fqdnUrl, 
            bearerToken,
            ct: cancellationToken));

        var datasetsTask = FetchSafelyAsync(() => _supersetApi.GetDatasetsAsync(
            fqdnUrl, 
            bearerToken,
            ct: cancellationToken));

        await Task.WhenAll(dashboardsTask, chartsTask, datasetsTask);

        var dashboards = (await dashboardsTask)?.Result.Select(d => d.Map()) ?? Enumerable.Empty<SupersetDashboardDto>();
        var charts = (await chartsTask)?.Result.Select(c => c.Map()) ?? Enumerable.Empty<SupersetChartDto>();
        var datasets = (await datasetsTask)?.Result.Select(ds => ds.Map()) ?? Enumerable.Empty<SupersetDatasetDto>();

        return (dashboards, charts, datasets);
    }
    
    private async Task<IEnumerable<SupersetSavedQueryDto>> FetchUserSavedQueriesFromApiAsync(
        Guid userId, 
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        
        var user = await _mediator.Send(
            new GetUserTenantQuery(userId, tenantId), 
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        var supersetTenant = await _supersetTenantService
            .GetSupersetTenantByIdAsync(tenantId, cancellationToken);

        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(tenantId);
        }
        
        await _supersetTenantService.EnsureSupersetContainerActiveAsync(tenantId, cancellationToken);

        var adminToken = await _supersetAuthService.GetAdminToken(tenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        var fqdnUrl = new Uri(supersetTenant.Container.FqdnUrl);

        var queriesApiResponse = await FetchSafelyAsync(() => _supersetApi.GetSavedQueriesAsync(
            fqdnUrl, 
            bearerToken, 
            SupersetQueryApiResolver.OwnerFilter(user.Id, tenantId),
            cancellationToken));

        return queriesApiResponse?.Result.Select(q => q.Map()) ?? Enumerable.Empty<SupersetSavedQueryDto>();
    }

    private static async Task<SupersetListApiResponse<T>?> FetchSafelyAsync<T>(Func<Task<SupersetListApiResponse<T>>> fetchApi)
    {
        try
        {
            return await fetchApi();
        }
        catch
        {
            return null;
        }
    }
    
}