using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Services;

internal class SupersetTenantService : ISupersetTenantService
{
    private readonly ISupersetTenantInstanceLifecycleService _supersetTenantInstanceLifecycleService;
    private readonly ILogger<SupersetTenantService> _logger;
    private readonly ISupersetRepository _supersetRepository;
    private readonly HybridCache _cache;

    public SupersetTenantService(
        ILogger<SupersetTenantService> logger,
        ISupersetRepository supersetRepository,
        HybridCache cache,
        ISupersetTenantInstanceLifecycleService supersetTenantInstanceLifecycleService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetRepository = supersetRepository ?? throw new ArgumentNullException(nameof(supersetRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _supersetTenantInstanceLifecycleService = supersetTenantInstanceLifecycleService ?? 
                                                  throw new ArgumentNullException(nameof(supersetTenantInstanceLifecycleService));
    }

    public async Task<SupersetTenant?> GetSupersetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        return await _cache.GetOrCreateAsync(
            SupersetCache.GetTenantConfigByTenantIdCacheKey(tenantId),
            async token => await _supersetRepository.GetSupersetTenantByIdAsync(tenantId, token),
            SupersetCache.ConfigCacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<SupersetTenant?> GetSupersetTenantByResourceIdAsync(
        string resourceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("ResourceId is required.", nameof(resourceId));

        var normalizedResourceId = SupersetCache.NormalizeResourceId(resourceId);

        return await _cache.GetOrCreateAsync(
            SupersetCache.GetTenantConfigByResourceIdCacheKey(normalizedResourceId),
            async token => await _supersetRepository.GetSupersetTenantByResourceIdAsync(normalizedResourceId, token),
            SupersetCache.ConfigCacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task SaveAsync(
        SupersetTenant tenant,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        await RemoveTenantConfigCacheAsync(
            tenant.TenantId,
            tenant.Container.ResourceId,
            cancellationToken);

        await _supersetRepository.SaveAsync(tenant, cancellationToken);

    }

    public async Task SaveSupersetTenantInstanceAsync(
        SupersetTenantInstance supersetTenantInstance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supersetTenantInstance);

        await RemoveTenantInstanceCacheAsync(
            supersetTenantInstance.TenantId,
            cancellationToken);

        await _cache.SetAsync(
            SupersetCache.GetTenantInstanceCacheKey(supersetTenantInstance.TenantId, supersetTenantInstance.Status),
            supersetTenantInstance,
            SupersetCache.InstanceCacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<SupersetTenantInstance?> GetSupersetTenantInstance(
        Guid tenantId,
        SupersetContainerInstanceStatus? status,
        CancellationToken cancellationToken = default)
    {
        
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (status is not null)
        {
            var cachedInstance = await _cache.GetOrCreateAsync(
                SupersetCache.GetTenantInstanceCacheKey(tenantId, status.Value),
                _ => ValueTask.FromResult<SupersetTenantInstance?>(null),
                SupersetCache.InstanceCacheOptions,
                cancellationToken: cancellationToken);

            if (cachedInstance is not null)
                return cachedInstance;

            var instanceFromAzure = await _supersetTenantInstanceLifecycleService
                .GetSupersetTenantInstance(tenantId, cancellationToken);

            if (instanceFromAzure is null)
                return null;

            await SaveSupersetTenantInstanceAsync(instanceFromAzure, cancellationToken);
            return instanceFromAzure.Status == status.Value ? instanceFromAzure : null;
        }
        
        foreach (var currentStatus in Enum.GetValues<SupersetContainerInstanceStatus>())
        {
            var instance = await _cache.GetOrCreateAsync(
                SupersetCache.GetTenantInstanceCacheKey(tenantId, currentStatus),
                _ => ValueTask.FromResult<SupersetTenantInstance?>(null),
                SupersetCache.InstanceCacheOptions,
                cancellationToken: cancellationToken);

            if (instance is not null)
                return instance;
        }
        
        var instanceFromAzureOrDb = await GetTenantInstanceFromAzureOrDbAsync(tenantId, cancellationToken);
        if (instanceFromAzureOrDb is not null)
        {
            await SaveSupersetTenantInstanceAsync(instanceFromAzureOrDb, cancellationToken);
            return instanceFromAzureOrDb;
        }

        return null;
    }

    public async Task EnsureSupersetContainerActiveAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var tenantSuperset = await GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (tenantSuperset is null)
            throw new SupersetTenantNotFoundException(tenantId);

        if(tenantSuperset.Status != SupersetTenantStatus.Active)
        {
            throw new SupersetTenantBlockedException(
                tenantSuperset.Id, 
                tenantSuperset.Status);
        }
    }

    private async Task<SupersetTenantInstance?> GetTenantInstanceFromAzureOrDbAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var supersetTenant = await GetSupersetTenantByIdAsync(tenantId, cancellationToken);
        if (supersetTenant is null)
            return null;
        var resourceId = supersetTenant.Container.ResourceId;
        
        var instance = await _supersetTenantInstanceLifecycleService.IsContainerActiveAsync(
            resourceId,
            cancellationToken);
        
        if (!instance)
            return null;
        
        return SupersetTenantInstance.Create(supersetTenant, SupersetContainerInstanceStatus.Running);
    }

  private async Task RemoveTenantConfigCacheAsync(
        Guid tenantId,
        string resourceId,
        CancellationToken cancellationToken)
    {
        var normalizedResourceId = SupersetCache.NormalizeResourceId(resourceId);

        await Task.WhenAll(
            _cache.RemoveAsync(SupersetCache.GetTenantConfigByTenantIdCacheKey(tenantId), cancellationToken).AsTask(),
            _cache.RemoveAsync(SupersetCache.GetTenantConfigByResourceIdCacheKey(normalizedResourceId), cancellationToken).AsTask());
        
    }

    private async Task RemoveTenantInstanceCacheAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var removeTasks = Enum.GetValues<SupersetContainerInstanceStatus>()
            .Select(status => _cache.RemoveAsync(SupersetCache.GetTenantInstanceCacheKey(tenantId, status), cancellationToken).AsTask());

        await Task.WhenAll(removeTasks);
    }
}