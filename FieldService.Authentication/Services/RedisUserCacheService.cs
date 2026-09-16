using System.Text.Json;
using FieldService.Authentication.Dtos;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Cache.Interfaces;
using FieldService.Shared.Types;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RoleType = FieldService.Shared.Types.Role;

namespace FieldService.Authentication.Services;

internal sealed class RedisUserCacheService(
    IRedisContext redisContext,
    IOptions<AuthenticationOptions> options) : IUserCacheService
{
    private const string AuthenticationPrefix = "authentication:";
    private const string UserPrefix = $"{AuthenticationPrefix}user:";
    private const string UserExternalPrefix = $"{AuthenticationPrefix}external:";

    private const string UpsertUserLuaScript = @"
        local userKey = ARGV[1]
        local currentExternalKey = ARGV[2]
        local previousExternalKey = ARGV[3]
        local payload = ARGV[4]
        local userId = ARGV[5]
        local ttlMs = tonumber(ARGV[6])

        redis.call('PSETEX', userKey, ttlMs, payload)

        if previousExternalKey ~= '' and previousExternalKey ~= currentExternalKey then
            redis.call('DEL', previousExternalKey)
        end

        if currentExternalKey ~= '' then
            redis.call('PSETEX', currentExternalKey, ttlMs, userId)
        end

        return 1
    ";

    private const string RemoveUserLuaScript = @"
        local userKey = ARGV[1]
        local externalKey = ARGV[2]

        redis.call('DEL', userKey)

        if externalKey ~= '' then
            redis.call('DEL', externalKey)
        end

        return 1
    ";

    private readonly TimeSpan _cacheTtl =
        TimeSpan.FromMinutes(options.Value.Session.TokenLifetimeInMinutes) +
        TimeSpan.FromHours(options.Value.Session.CacheTtlExtraHours);

    private sealed record RedisUserCacheEntry(UserAuthenticationDto User, string? ExternalId);

    public async Task<UserAuthenticationDto?> GetUserByExternalIdAsync(
        string externalId, 
        CancellationToken ct = default)
    {
        string.IsNullOrWhiteSpace(externalId);
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

        return await GetUserByIdAsync(userId, ct);
    }

    public async Task SaveUserAsync(
        UserAuthenticationDto user,
        string? externalId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        var existingEntry = await TryGetCacheEntryAsync(user.UserId, ct);
        var effectiveExternalId = string.IsNullOrWhiteSpace(externalId)
            ? existingEntry?.ExternalId
            : externalId;
        var userKey = GetUserKey(user.UserId);
        var currentExternalKey = GetOptionalUserExternalKey(effectiveExternalId);
        var previousExternalKey = GetOptionalUserExternalKey(existingEntry?.ExternalId);
        var payload = JsonSerializer.Serialize(new RedisUserCacheEntry(user, effectiveExternalId));
        var ttlMs = Math.Max(1L, (long)Math.Ceiling(_cacheTtl.TotalMilliseconds));

        await redisContext.Database.ScriptEvaluateAsync(
            UpsertUserLuaScript,
            values:
            [
                userKey,
                currentExternalKey,
                previousExternalKey,
                payload,
                user.UserId.ToString("N"),
                ttlMs.ToString()
            ]);
    }

    public async Task<UserAuthenticationDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        ValidateUserId(userId);
        ct.ThrowIfCancellationRequested();

        var cachedValue = await redisContext.Database.StringGetAsync(GetUserKey(userId));
        if (!cachedValue.HasValue)
            return null;

        return DeserializeCacheEntry(cachedValue!)?.User;
    }

    public async Task<UserAuthenticationDto?> GetUserTenantsByExternalIdAsync(
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

        return await GetUserByIdAsync(userId, ct);
    }

    public async Task RemoveUserAsync(Guid userId, CancellationToken ct = default)
    {
        ValidateUserId(userId);
        ct.ThrowIfCancellationRequested();

        var existingEntry = await TryGetCacheEntryAsync(userId, ct);

        await redisContext.Database.ScriptEvaluateAsync(
            RemoveUserLuaScript,
            values:
            [
                GetUserKey(userId),
                GetOptionalUserExternalKey(existingEntry?.ExternalId)
            ]);
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

    private static string GetOptionalUserExternalKey(string? externalId) =>
        string.IsNullOrWhiteSpace(externalId)
            ? string.Empty
            : GetUserExternalKey(externalId);

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

    private async Task<RedisUserCacheEntry?> TryGetCacheEntryAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var cachedValue = await redisContext.Database.StringGetAsync(GetUserKey(userId));
        return cachedValue.HasValue
            ? DeserializeCacheEntry(cachedValue!)
            : null;
    }

    private static RedisUserCacheEntry? DeserializeCacheEntry(RedisValue cachedValue)
    {
        var payload = cachedValue.ToString();
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            var entry = JsonSerializer.Deserialize<RedisUserCacheEntry>(payload);
            if (entry?.User != null)
                return entry;
        }
        catch (JsonException)
        {
        }

        try
        {
            var user = JsonSerializer.Deserialize<UserAuthenticationDto>(payload);
            if (user != null)
                return new RedisUserCacheEntry(user, null);
        }
        catch (JsonException)
        {
        }
        return null;
    }

}
