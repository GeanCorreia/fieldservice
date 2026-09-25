using FieldService.Authentication.Entities;
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
    private const string SessionJwtIdSuffix = ":jwtId";

    private const int FallbackBatchSize = 100;
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
        Session session,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ct.ThrowIfCancellationRequested();
        
        await _sessionRepository.Save(session, ct);
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

    public async Task SaveSessionsAsync(
        IEnumerable<Session> sessions, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ct.ThrowIfCancellationRequested();

        var sessionsList = sessions
            .GroupBy(static session => session.Id)
            .Select(static group => group.Last())
            .ToList();

        if (sessionsList.Count == 0)
            return;

        await _sessionRepository.Save(sessionsList, ct);

        var sessionIds = sessionsList.Select(static session => session.Id).ToArray();

        foreach (var chunk in sessionIds.Chunk(FallbackBatchSize))
        {
            var removeTasks = new List<Task>(chunk.Length * 2);

            foreach (var sessionId in chunk)
            {
                removeTasks.Add(_cache.RemoveAsync(GetSessionKey(sessionId), ct).AsTask());
                removeTasks.Add(_cache.RemoveAsync(GetSessionJwtIdKey(sessionId), ct).AsTask());
            }

            await Task.WhenAll(removeTasks);
        }

        await RemoveSessionsFromIndexAsync(sessionIds, ct);
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

        session.UpdateExpiration(expiresAt);
        await _sessionRepository.Save(session, ct);
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
            session,
            options: cacheOptions,
            cancellationToken: ct);
    }

    public async Task<Session?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        ct.ThrowIfCancellationRequested();

        var session = await _cache.GetOrCreateAsync<Session?>(
            GetSessionKey(sessionId),
            async cancel =>
            {
                var entity = await _sessionRepository.GetById(sessionId, cancel);
                return entity is null ? null : entity;
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

        
    private static string GetSessionKey(Guid sessionId) => $"{SessionPrefix}{sessionId:N}";
    private static string GetSessionJwtIdKey(Guid sessionId) => $"{SessionPrefix}{sessionId:N}{SessionJwtIdSuffix}";
    

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

    private async Task RemoveSessionsFromIndexAsync(IEnumerable<Guid> sessionIdsToRemove, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sessionIdsToRemove);

        var idsToRemoveSet = sessionIdsToRemove.ToHashSet();
        if (idsToRemoveSet.Count == 0)
            return;

        var sessionIds = await GetSessionIndexAsync(ct);
        if (sessionIds.RemoveAll(idsToRemoveSet.Contains) == 0)
            return;

        await _cache.SetAsync(
            SessionsIndexKey,
            sessionIds,
            options: SessionsIndexCacheOptions,
            cancellationToken: ct);
    }

    

    

    private async Task RemoveSessionCacheEntriesAsync(Guid sessionId, CancellationToken ct, bool removeFromIndex = true)
    {
        await _cache.RemoveAsync(GetSessionKey(sessionId), ct);
        await _cache.RemoveAsync(GetSessionJwtIdKey(sessionId), ct);

        if (removeFromIndex)
            await RemoveSessionFromIndexAsync(sessionId, ct);
    }

    
    private TimeSpan GetSessionTtl(DateTimeOffset expiresAt)
    {
        var ttl = expiresAt - DateTimeService.GetNow() + _cacheTtlExtra;
        return ttl < TimeSpan.Zero ? TimeSpan.Zero : ttl;
    }
}