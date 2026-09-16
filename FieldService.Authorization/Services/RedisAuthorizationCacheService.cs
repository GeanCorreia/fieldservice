using System.Text.Json;
using FieldService.Authorization.Dtos;
using FieldService.Authorization.Interfaces;
using FieldService.Cache.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FieldService.Authorization.Services;

internal sealed class RedisAuthorizationCacheService(IRedisContext redisContext, IConfiguration configuration) : IAuthorizationCacheService
{
    private const string UserContextPrefix = "authorization:user:";
    private readonly TimeSpan _cacheTtl = GetCacheTtl(configuration);

    public async Task<UserAuthorizationDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        

        ct.ThrowIfCancellationRequested();

        var key = GenerateKey(userId);
        var cachedValue = await redisContext.Database.StringGetAsync(key);

        if (!cachedValue.HasValue)
            return null;

        try
        {
            var snapshot = JsonSerializer.Deserialize<UserAuthorizationDto>(cachedValue!.ToString());
            return snapshot;
        }
        catch
        {
            await redisContext.Database.KeyDeleteAsync(key);
            return null;
        }
    }

    public async Task SaveUserAsync(UserAuthorizationDto user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        var key = GenerateKey(user.UserId);
        var serialized = JsonSerializer.Serialize(user);

        await redisContext.Database.StringSetAsync(key, serialized, _cacheTtl);
    }

    private static string GenerateKey(Guid userId)
    {
        return $"{UserContextPrefix}{userId:N}";
    }

    private static TimeSpan GetCacheTtl(IConfiguration configuration)
    {
        var seconds = configuration.GetValue<int?>("Authorization:CacheExpirationInSeconds") ?? 86400;
        return TimeSpan.FromSeconds(seconds);
    }
}
