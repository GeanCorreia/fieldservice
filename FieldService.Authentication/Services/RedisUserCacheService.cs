using System.Text.Json;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Cache.Interfaces;
using Microsoft.Extensions.Options;

namespace FieldService.Authentication.Services;

internal sealed class RedisUserCacheService(
    IRedisContext redisContext,
    IOptions<AuthenticationOptions> options) : IUserCacheService
{
    private const string UserPrefix = "user:";
    private const string UserExternalPrefix = "user:external:";
    private readonly TimeSpan _cacheTtl =
        TimeSpan.FromMinutes(options.Value.Session.TokenLifetimeInMinutes) +
        TimeSpan.FromHours(options.Value.Session.CacheTtlExtraHours);

    public async Task SaveUserAsync(UserAuthenticationCacheModel user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var userKey = GetUserKey(user.UserId);
        var externalKey = GetUserExternalKey(user.ExternalId);
        var payload = JsonSerializer.Serialize(user);

        var existingPayload = await db.StringGetAsync(userKey);
        if (existingPayload.HasValue)
        {
            var existing = JsonSerializer.Deserialize<UserAuthenticationCacheModel>(existingPayload!);
            if (existing != null &&
                existing.ExternalId != user.ExternalId)
            {
                await db.KeyDeleteAsync(GetUserExternalKey(existing.ExternalId));
            }
        }

        await db.StringSetAsync(userKey, payload, _cacheTtl);
        await db.StringSetAsync(externalKey, user.UserId.ToString("N"), _cacheTtl);
    }

    public async Task<UserAuthenticationCacheModel?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        ValidateUserId(userId);
        ct.ThrowIfCancellationRequested();

        var cachedValue = await redisContext.Database.StringGetAsync(GetUserKey(userId));
        if (!cachedValue.HasValue)
            return null;

        return JsonSerializer.Deserialize<UserAuthenticationCacheModel>(cachedValue!);
    }

    public async Task<UserAuthenticationCacheModel?> GetUserByExternalIdAsync(
        string externalId,
        CancellationToken ct = default)
    {
        ValidateExternalId(externalId);
        ct.ThrowIfCancellationRequested();

        var externalKey = GetUserExternalKey(externalId);
        var userIdValue = await redisContext.Database.StringGetAsync(externalKey);
        if (!userIdValue.HasValue)
            return null;

        if (!Guid.TryParse(userIdValue.ToString(), out var userId))
        {
            await redisContext.Database.KeyDeleteAsync(externalKey);
            return null;
        }

        return await GetUserAsync(userId, ct);
    }

    public async Task RemoveUserAsync(Guid userId, CancellationToken ct = default)
    {
        ValidateUserId(userId);
        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var userKey = GetUserKey(userId);
        var cachedValue = await db.StringGetAsync(userKey);
        if (cachedValue.HasValue)
        {
            var cachedUser = JsonSerializer.Deserialize<UserAuthenticationCacheModel>(cachedValue!);
            if (cachedUser != null)
                await db.KeyDeleteAsync(GetUserExternalKey(cachedUser.ExternalId));
        }

        await db.KeyDeleteAsync(userKey);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default)
    {
        ValidateUserId(userId);
        ct.ThrowIfCancellationRequested();

        return await redisContext.Database.KeyExistsAsync(GetUserKey(userId));
    }

    private static string GetUserKey(Guid userId) => $"{UserPrefix}{userId:N}";
    private static string GetUserExternalKey(string externalId) =>
        $"{UserExternalPrefix}{Uri.EscapeDataString(externalId.Trim())}";

    private static void ValidateUserId(Guid userId)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
    }

    private static void ValidateExternalId(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
    }

}
