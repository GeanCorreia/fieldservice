using System.Text.Json;
using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Types;
using FieldService.Cache.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FieldService.Authorization.Services;

internal sealed class RedisAuthorizationCacheService(IRedisContext redisContext, IConfiguration configuration) : IAuthorizationCacheService
{
    private const string UserContextPrefix = "authorization:usercontext:";
    private readonly TimeSpan _cacheTtl = GetCacheTtl(configuration);

    public async Task<UserAuthorizationSnapshot?> GetUserContext(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ct.ThrowIfCancellationRequested();

        var key = GenerateKey(userId, tenantId);
        var cachedValue = await redisContext.Database.StringGetAsync(key);

        if (!cachedValue.HasValue)
            return null;

        try
        {
            var snapshot = JsonSerializer.Deserialize<UserAuthorizationSnapshot>(cachedValue!.ToString());
            return snapshot;
        }
        catch
        {
            // If deserialization fails, treat as cache miss
            await redisContext.Database.KeyDeleteAsync(key);
            return null;
        }
    }

    public async Task SaveUserContext(UserAuthorizationSnapshot userContext, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userContext);
        ct.ThrowIfCancellationRequested();

        var key = GenerateKey(userContext.UserId, userContext.TenantId);
        var serialized = JsonSerializer.Serialize(userContext);

        await redisContext.Database.StringSetAsync(key, serialized, _cacheTtl);
    }

    private static string GenerateKey(Guid userId, Guid tenantId)
    {
        return $"{UserContextPrefix}{userId:N}:{tenantId:N}";
    }

    private static TimeSpan GetCacheTtl(IConfiguration configuration)
    {
        var seconds = configuration.GetValue<int?>("Authorization:CacheExpirationInSeconds") ?? 86400;
        return TimeSpan.FromSeconds(seconds);
    }
}
