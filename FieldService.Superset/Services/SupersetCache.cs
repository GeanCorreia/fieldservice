using FieldService.Superset.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace FieldService.Superset.Services;

internal static class SupersetCache
{
    public static readonly HybridCacheEntryOptions ConfigCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(12),
        LocalCacheExpiration = TimeSpan.FromHours(1)
    };

    public static readonly HybridCacheEntryOptions InstanceCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(4),
        LocalCacheExpiration = TimeSpan.FromMinutes(30)
    };
    
    public static string GetTenantConfigByTenantIdCacheKey(Guid tenantId) =>
        $"superset:tenant-config:tenant-id:{tenantId:N}";

    public static string GetTenantConfigByResourceIdCacheKey(string resourceId) =>
        $"superset:tenant-config:resource-id:{NormalizeResourceId(resourceId)}";

    public static string GetTenantInstanceCacheKey(Guid tenantId, SupersetInstanceStatus status) =>
        $"superset:tenant-instance:tenant-id:{tenantId:N}:status:{status}";

    public static string NormalizeResourceId(string resourceId) => resourceId.Trim().ToLowerInvariant();
}

