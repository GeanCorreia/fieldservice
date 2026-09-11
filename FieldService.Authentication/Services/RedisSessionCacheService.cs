using System.Text.Json;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Cache.Interfaces;
using FieldService.Shared.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FieldService.Authentication.Services;

internal sealed class RedisSessionCacheService(
    IRedisContext redisContext,
    IOptions<AuthenticationOptions> options) : ISessionCacheService
{
    private const string AuthenticationPrefix = "authentication:";
    private const string SessionPrefix = $"{AuthenticationPrefix}session:";
    private const string LastActivitySuffix = ":lastActivity";
    private const string SessionJwtIdSuffix = ":jwtId";
    private const string ActivitiesSuffix = ":activities";
    private readonly TimeSpan _cacheTtlExtra = TimeSpan.FromHours(options.Value.Session.CacheTtlExtraHours);

    public async Task SaveSessionAsync(SessionCacheModel session, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ct.ThrowIfCancellationRequested();

        var ttl = GetSessionTtl(session.ExpiresAt);
        var db = redisContext.Database;

        await db.StringSetAsync(GetSessionKey(session.Id), JsonSerializer.Serialize(session), ttl);
    }

    public async Task TouchSessionAsync(
        Guid sessionId,
        SessionActivityCacheModel activity,
        CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ArgumentNullException.ThrowIfNull(activity);
        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var sessionKey = GetSessionKey(sessionId);
        var ttl = await db.KeyTimeToLiveAsync(sessionKey);
        if (ttl is null)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        await db.ListRightPushAsync(GetActivitiesKey(sessionId), JsonSerializer.Serialize(activity));
        await db.StringSetAsync(GetLastActivityKey(sessionId), activity.Timestamp.ToString("O"), ttl);
        await db.KeyExpireAsync(GetActivitiesKey(sessionId), ttl);
    }

    public async Task<SessionCacheModel?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        var cachedValue = await redisContext.Database.StringGetAsync(GetSessionKey(sessionId));
        if (!cachedValue.HasValue)
            return null;

        return JsonSerializer.Deserialize<SessionCacheModel>(cachedValue!);
    }

    public async Task RemoveSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        await redisContext.Database.KeyDeleteAsync(new[]
        {
            (RedisKey)GetSessionKey(sessionId),
            (RedisKey)GetLastActivityKey(sessionId),
            (RedisKey)GetSessionJwtIdKey(sessionId),
            (RedisKey)GetActivitiesKey(sessionId)
        });
    }

    public async Task<string?> GetSessionJwtIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        var cachedValue = await redisContext.Database.StringGetAsync(GetSessionJwtIdKey(sessionId));
        return cachedValue.HasValue ? cachedValue.ToString() : null;
    }

    public async Task UpdateSessionJwtIdAsync(
        Guid sessionId,
        string jwtId,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(jwtId))
            throw new ArgumentException("JwtId is required.", nameof(jwtId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var sessionKey = GetSessionKey(sessionId);

        var sessionExists = await db.KeyExistsAsync(sessionKey);
        if (!sessionExists)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var ttl = GetSessionTtl(expiresAt);

        await db.StringSetAsync(GetSessionJwtIdKey(sessionId), jwtId, ttl);

        var cachedSession = await db.StringGetAsync(sessionKey);
        if (!cachedSession.HasValue)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var session = JsonSerializer.Deserialize<SessionCacheModel>(cachedSession!);
        if (session is null)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        await db.StringSetAsync(sessionKey, JsonSerializer.Serialize(session with { ExpiresAt = expiresAt }), ttl);
        await db.KeyExpireAsync(GetLastActivityKey(sessionId), ttl);
        await db.KeyExpireAsync(GetActivitiesKey(sessionId), ttl);
    }

    public async Task<bool> ExistsAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        return await redisContext.Database.KeyExistsAsync(GetSessionKey(sessionId));
    }

    public async Task<IEnumerable<SessionCacheModel>> GetInactiveCandidatesAsync(
        TimeSpan inactivityThreshold,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cutoffTime = DateTimeService.GetNow() - inactivityThreshold;
        var sessions = new List<SessionCacheModel>();

        var server = redisContext.Connection.GetServer(redisContext.Connection.GetEndPoints().First());
        var pattern = $"{SessionPrefix}*{LastActivitySuffix}";
        var db = redisContext.Database;

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            ct.ThrowIfCancellationRequested();

            var keyValue = key.ToString();
            if (!DateTimeOffset.TryParse((await db.StringGetAsync(key)).ToString(), out var lastActivityAt))
                continue;

            if (lastActivityAt >= cutoffTime)
                continue;

            var sessionIdToken = keyValue
                .Replace(SessionPrefix, string.Empty, StringComparison.Ordinal)
                .Replace(LastActivitySuffix, string.Empty, StringComparison.Ordinal);
            if (!Guid.TryParse(sessionIdToken, out var sessionId))
                continue;

            var session = await GetSessionAsync(sessionId, ct);
            if (session is not null)
                sessions.Add(session);
        }

        return sessions;
    }

    private static string GetSessionKey(Guid sessionId) => $"{SessionPrefix}{sessionId:N}";
    private static string GetLastActivityKey(Guid sessionId) => $"{SessionPrefix}{sessionId:N}{LastActivitySuffix}";
    private static string GetSessionJwtIdKey(Guid sessionId) => $"{SessionPrefix}{sessionId:N}{SessionJwtIdSuffix}";
    private static string GetActivitiesKey(Guid sessionId) => $"{SessionPrefix}{sessionId:N}{ActivitiesSuffix}";

    private TimeSpan GetSessionTtl(DateTimeOffset expiresAt)
    {
        var ttl = expiresAt - DateTimeService.GetNow() + _cacheTtlExtra;
        return ttl < TimeSpan.Zero ? TimeSpan.Zero : ttl;
    }
}
