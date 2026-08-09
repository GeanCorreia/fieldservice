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
    private const string SessionPrefix = "session:";
    private const string LastActivitySuffix = ":lastActivity";
    private const string SessionJwtIdSuffix = ":jwtId";
    private const string ActivitiesSuffix = ":activities";
    private readonly TimeSpan _cacheTtlExtra = TimeSpan.FromHours(options.Value.Session.CacheTtlExtraHours);

    public async Task SaveSessionAsync(SessionCacheModel session, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ct.ThrowIfCancellationRequested();

        var ttl = GetSessionTtl(session.ExpiresAt);
        var sessionKey = GetSessionKey(session.Id);
        var lastActivityKey = GetLastActivityKey(session.Id);
        var sessionJwtIdKey = GetSessionJwtIdKey(session.Id);
        var activitiesKey = GetActivitiesKey(session.Id);
        var effectiveLastActivityAt = session.LastActivityAt ?? GetLatestActivityTimestamp(session.Activities);
        var sessionSnapshot = session with
        {
            LastActivityAt = effectiveLastActivityAt,
            Activities = Array.Empty<SessionActivityCacheModel>()
        };
        var serialized = JsonSerializer.Serialize(sessionSnapshot);

        var db = redisContext.Database;
        await db.StringSetAsync(sessionKey, serialized, ttl);

        await db.KeyDeleteAsync(activitiesKey);
        if (session.Activities.Count > 0)
        {
            var serializedActivities = session.Activities
                .Select(activity => (RedisValue)JsonSerializer.Serialize(activity))
                .ToArray();
            await db.ListRightPushAsync(activitiesKey, serializedActivities);
            await db.KeyExpireAsync(activitiesKey, ttl);
        }

        if (effectiveLastActivityAt.HasValue)
            await db.StringSetAsync(lastActivityKey, effectiveLastActivityAt.Value.ToString("O"), ttl);
        else
            await db.KeyDeleteAsync(lastActivityKey);

        var currentJwtId = GetCurrentJwtId(session);
        if (string.IsNullOrWhiteSpace(currentJwtId))
        {
            await db.KeyDeleteAsync(sessionJwtIdKey);
            return;
        }

        await db.StringSetAsync(sessionJwtIdKey, currentJwtId, ttl);
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
        var exists = await db.KeyExistsAsync(sessionKey);
        if (!exists)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var ttl = await db.KeyTimeToLiveAsync(sessionKey);
        var activitiesKey = GetActivitiesKey(sessionId);
        var lastActivityKey = GetLastActivityKey(sessionId);

        await db.ListRightPushAsync(activitiesKey, JsonSerializer.Serialize(activity));
        await db.StringSetAsync(lastActivityKey, activity.Timestamp.ToString("O"), ttl);
        await db.KeyExpireAsync(activitiesKey, ttl);
    }

    public async Task<SessionCacheModel?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        var cachedValue = await db.StringGetAsync(GetSessionKey(sessionId));
        if (!cachedValue.HasValue)
            return null;

        var session = JsonSerializer.Deserialize<SessionCacheModel>(cachedValue!);
        if (session is null)
            return null;

        var activitiesValues = await db.ListRangeAsync(GetActivitiesKey(sessionId));
        IReadOnlyCollection<SessionActivityCacheModel> activities = session.Activities;
        if (activitiesValues.Length > 0)
        {
            activities = activitiesValues
                .Select(value => JsonSerializer.Deserialize<SessionActivityCacheModel>(value!))
                .OfType<SessionActivityCacheModel>()
                .ToArray();
        }

        var lastActivityAt = session.LastActivityAt;
        var lastActivityValue = await db.StringGetAsync(GetLastActivityKey(sessionId));
        if (lastActivityValue.HasValue && DateTime.TryParse(lastActivityValue.ToString(), out var parsedLastActivity))
            lastActivityAt = parsedLastActivity;

        return session with
        {
            LastActivityAt = lastActivityAt,
            Activities = activities
        };
    }

    public async Task RemoveSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        var db = redisContext.Database;
        await db.KeyDeleteAsync(new[]
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
        if (!cachedValue.HasValue)
            return null;

        return cachedValue.ToString();
    }

    public async Task UpdateSessionJwtIdAsync(
        Guid sessionId,
        string jwtId,
        DateTime expiresAt,
        CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(jwtId))
            throw new ArgumentException("JwtId is required.", nameof(jwtId));

        ct.ThrowIfCancellationRequested();

        var sessionKey = GetSessionKey(sessionId);
        var sessionJwtIdKey = GetSessionJwtIdKey(sessionId);
        var db = redisContext.Database;

        var sessionExists = await db.KeyExistsAsync(sessionKey);
        if (!sessionExists)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var ttl = GetSessionTtl(expiresAt);
        await db.StringSetAsync(sessionJwtIdKey, jwtId, ttl);

        var cachedSession = await db.StringGetAsync(sessionKey);
        if (!cachedSession.HasValue)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var session = JsonSerializer.Deserialize<SessionCacheModel>(cachedSession!);
        if (session is null)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var updatedSession = session with { ExpiresAt = expiresAt };
        await db.StringSetAsync(sessionKey, JsonSerializer.Serialize(updatedSession), ttl);

        await db.KeyExpireAsync(GetActivitiesKey(sessionId), ttl);
        await db.KeyExpireAsync(GetLastActivityKey(sessionId), ttl);
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
            if (!DateTime.TryParse((await db.StringGetAsync(key)).ToString(), out var lastActivityAt))
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

    private TimeSpan GetSessionTtl(DateTime expiresAt)
    {
        var ttl = expiresAt - DateTimeService.GetNow() + _cacheTtlExtra;
        return ttl < TimeSpan.Zero ? TimeSpan.Zero : ttl;
    }

    private static string? GetCurrentJwtId(SessionCacheModel session)
    {
        SessionActivityCacheModel? latest = null;
        foreach (var activity in session.Activities)
        {
            if (latest is null || activity.Timestamp > latest.Timestamp)
                latest = activity;
        }

        return latest?.JwtId;
    }

    private static DateTime? GetLatestActivityTimestamp(IReadOnlyCollection<SessionActivityCacheModel> activities)
    {
        SessionActivityCacheModel? latest = null;
        foreach (var activity in activities)
        {
            if (latest is null || activity.Timestamp > latest.Timestamp)
                latest = activity;
        }

        return latest?.Timestamp;
    }

}
