using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;


namespace FieldService.Authentication.Services;

public class SessionService : ISessionService
{
    private const string AuthenticationPrefix = "authentication:";
    private const string SessionPrefix = $"{AuthenticationPrefix}session:";
    private const string SessionsIndexKey = $"{AuthenticationPrefix}sessions:index";
    private const string LastActivitySuffix = ":lastActivity";
    private const string SessionJwtIdSuffix = ":jwtId";
    private const string ActivitiesSuffix = ":activities";
    private readonly TimeSpan _cacheTtlExtra;
   
    private readonly HybridCache _cache;
    private readonly ISessionRepository _sessionRepository;

    public SessionService(
        IOptions<AuthenticationOptions> options,
        HybridCache cache,
        ISessionRepository sessionRepository)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheTtlExtra = TimeSpan.FromHours(options.Value.Session.CacheTtlExtraHours);
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task SaveSessionAsync(
        SessionCacheModel session,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ct.ThrowIfCancellationRequested();
        
        await _sessionRepository.Save(Map(session), ct);
        await AddSessionToIndexAsync(session.Id, ct);

        var ttl = GetSessionTtl(session.ExpiresAt);
        if (ttl <= TimeSpan.Zero)
        {
            await RemoveSessionCacheEntriesAsync(session.Id, ct, removeFromIndex: false);
            return;
        }

        await _cache.SetAsync(
            GetSessionKey(session.Id),
            session,
            options: CreateCacheOptions(ttl),
            cancellationToken: ct);
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

        var session = await GetSessionAsync(sessionId, ct);
        if (session is null)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var ttl = GetSessionTtl(session.ExpiresAt);
        if (ttl <= TimeSpan.Zero)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var activities = await GetActivitiesAsync(sessionId, ct);
        var updatedActivities = new List<SessionActivityCacheModel>(activities) { activity };

        await AddSessionToIndexAsync(sessionId, ct);
        await _cache.SetAsync(
            GetActivitiesKey(sessionId),
            updatedActivities,
            options: CreateCacheOptions(ttl),
            cancellationToken: ct);
        await _cache.SetAsync(
            GetLastActivityKey(sessionId),
            activity.Timestamp,
            options: CreateCacheOptions(ttl),
            cancellationToken: ct);
    }

    public async Task<string?> GetSessionJwtIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        return await _cache.GetOrCreateAsync<string?>(
            GetSessionJwtIdKey(sessionId),
            _ => ValueTask.FromResult<string?>(default),
            cancellationToken: ct);
    }

    public async Task UpdateSessionJwtIdAsync(Guid sessionId, string jwtId, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(jwtId))
            throw new ArgumentException("JwtId is required.", nameof(jwtId));

        ct.ThrowIfCancellationRequested();

        var session = await GetSessionAsync(sessionId, ct);
        if (session is null)
            throw new KeyNotFoundException($"Session '{sessionId}' was not found in cache.");

        var updatedSession = session with { ExpiresAt = expiresAt };
        await _sessionRepository.Save(Map(updatedSession), ct);
        await AddSessionToIndexAsync(sessionId, ct);

        var ttl = GetSessionTtl(expiresAt);
        if (ttl <= TimeSpan.Zero)
        {
            await RemoveSessionCacheEntriesAsync(sessionId, ct, removeFromIndex: false);
            return;
        }

        var cacheOptions = CreateCacheOptions(ttl);

        await _cache.SetAsync(
            GetSessionJwtIdKey(sessionId),
            jwtId,
            options: cacheOptions,
            cancellationToken: ct);
        await _cache.SetAsync(
            GetSessionKey(sessionId),
            updatedSession,
            options: cacheOptions,
            cancellationToken: ct);

        var lastActivity = await GetLastActivityAsync(sessionId, ct);
        if (lastActivity.HasValue)
        {
            await _cache.SetAsync(
                GetLastActivityKey(sessionId),
                lastActivity.Value,
                options: cacheOptions,
                cancellationToken: ct);
        }

        var activities = await GetActivitiesAsync(sessionId, ct);
        if (activities.Count > 0)
        {
            await _cache.SetAsync(
                GetActivitiesKey(sessionId),
                activities,
                options: cacheOptions,
                cancellationToken: ct);
        }
    }

    public async Task<SessionCacheModel?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        var session = await _cache.GetOrCreateAsync<SessionCacheModel?>(
            GetSessionKey(sessionId),
            async cancel =>
            {
                var entity = await _sessionRepository.GetById(sessionId, cancel);
                return entity is null ? null : Map(entity);
            },
            cancellationToken: ct);

        if (session is null)
            return null;

        if (GetSessionTtl(session.ExpiresAt) > TimeSpan.Zero)
            return session;

        await RemoveSessionCacheEntriesAsync(sessionId, ct);
        return null;
    }

    public async Task RemoveSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        await RemoveSessionCacheEntriesAsync(sessionId, ct);
    }

    public async Task<bool> ExistsAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        return await GetSessionAsync(sessionId, ct) is not null;
    }

    public async Task<IEnumerable<SessionCacheModel>> GetInactiveCandidatesAsync(TimeSpan inactivityThreshold, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cutoffTime = DateTimeService.GetNow() - inactivityThreshold;
        var sessions = new List<SessionCacheModel>();
        var indexedSessions = await GetSessionIndexAsync(ct);

        foreach (var sessionId in indexedSessions)
        {
            ct.ThrowIfCancellationRequested();

            var lastActivityAt = await GetLastActivityAsync(sessionId, ct);
            if (lastActivityAt is null || lastActivityAt >= cutoffTime)
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

    private static HybridCacheEntryOptions CreateCacheOptions(
        TimeSpan ttl,
        TimeSpan? ttlExtra = null)
    {
 
        return new HybridCacheEntryOptions
        {
            Expiration = ttl + (ttlExtra ?? TimeSpan.Zero),
            LocalCacheExpiration = ttl + (ttlExtra ?? TimeSpan.Zero)
        };
    }
    
    private HybridCacheEntryOptions SessionsIndexCacheOptions =>
        
        new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromDays(7),
            LocalCacheExpiration = TimeSpan.FromDays(7)
        };

    private async Task<List<Guid>> GetSessionIndexAsync(CancellationToken ct)
    {
        var sessionIds = await _cache.GetOrCreateAsync(
            SessionsIndexKey,
            _ => ValueTask.FromResult(new List<Guid>()),
            options: SessionsIndexCacheOptions,
            cancellationToken: ct);

        return sessionIds;
    }

    private async Task AddSessionToIndexAsync(Guid sessionId, CancellationToken ct)
    {
        var sessionIds = await GetSessionIndexAsync(ct);
        if (sessionIds.Contains(sessionId))
            return;

        sessionIds.Add(sessionId);
        await _cache.SetAsync(
            SessionsIndexKey,
            sessionIds,
            options: SessionsIndexCacheOptions,
            cancellationToken: ct);
    }

    private async Task RemoveSessionFromIndexAsync(Guid sessionId, CancellationToken ct)
    {
        var sessionIds = await GetSessionIndexAsync(ct);
        if (sessionIds.RemoveAll(x => x == sessionId) == 0)
            return;

        await _cache.SetAsync(
            SessionsIndexKey,
            sessionIds,
            options: SessionsIndexCacheOptions,
            cancellationToken: ct);
    }

    private async Task<DateTimeOffset?> GetLastActivityAsync(Guid sessionId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync(
            GetLastActivityKey(sessionId),
            _ => ValueTask.FromResult<DateTimeOffset?>(default),
            cancellationToken: ct);
    }

    private async Task<List<SessionActivityCacheModel>> GetActivitiesAsync(Guid sessionId, CancellationToken ct)
    {
        var activities = await _cache.GetOrCreateAsync(
            GetActivitiesKey(sessionId),
            _ => ValueTask.FromResult(new List<SessionActivityCacheModel>()),
            cancellationToken: ct);

        return activities;
    }

    private async Task RemoveSessionCacheEntriesAsync(Guid sessionId, CancellationToken ct, bool removeFromIndex = true)
    {
        await _cache.RemoveAsync(GetSessionKey(sessionId), ct);
        await _cache.RemoveAsync(GetLastActivityKey(sessionId), ct);
        await _cache.RemoveAsync(GetSessionJwtIdKey(sessionId), ct);
        await _cache.RemoveAsync(GetActivitiesKey(sessionId), ct);

        if (removeFromIndex)
            await RemoveSessionFromIndexAsync(sessionId, ct);
    }

    private static Entities.Session Map(SessionCacheModel session)
    {
        return new Entities.Session(
            session.Id,
            session.UserId,
            session.TenantId,
            session.ExternalId,
            session.Provider,
            session.StartedAt,
            session.ExpiresAt,
            session.RevokedAt,
            session.RevocationReason);
    }

    private static SessionCacheModel Map(Entities.Session session)
    {
        return new SessionCacheModel(
            session.Id,
            session.UserId,
            session.TenantId,
            session.ExternalId,
            session.Provider,
            session.StartedAt,
            session.ExpiresAt,
            session.RevokedAt,
            session.RevocationReason);
    }

    private TimeSpan GetSessionTtl(DateTimeOffset expiresAt)
    {
        var ttl = expiresAt - DateTimeService.GetNow() + _cacheTtlExtra;
        return ttl < TimeSpan.Zero ? TimeSpan.Zero : ttl;
    }
}