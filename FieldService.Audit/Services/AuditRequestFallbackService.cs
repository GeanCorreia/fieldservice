using FieldService.Audit.Entities;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Concurrent;

namespace FiledService.Audit.Services;

public class AuditFallbackService : IAuditFallbackService, IAuditRequestFallbackService, IAuditChangeFallbackService
{
    private readonly HybridCache _cache;
    private readonly IAuditRequestRepository _auditRequestRepository;
    private readonly IAuditChangeRepository _auditChangeRepository;
    private const int FallbackBatchSize = 100;
    
    private const string AuditPrefix = "audit:";
    private const string RequestPrefix = $"{AuditPrefix}request:";
    private const string FallbackActivityKeyPrefix = $"{RequestPrefix}fallback:";
    private const string FallbackBucketPrefix = $"{RequestPrefix}fallback-bucket:";
    private const string FallbackCurrentBucketIdKey = $"{RequestPrefix}fallback-current-bucket-id";
    private const string FallbackCurrentBucketCountKey = $"{RequestPrefix}fallback-current-bucket-count";
    private const string FallbackDrainCursorKey = $"{RequestPrefix}fallback-drain-cursor";
    private const string ChangePrefix = $"{AuditPrefix}change:";
    private const string FallbackChangeKeyPrefix = $"{ChangePrefix}fallback:";
    private const string FallbackChangeIndexKey = $"{ChangePrefix}fallback-index";
    private const string AccessPrefix = $"{AuditPrefix}access:";
    private const string FallbackAccessKeyPrefix = $"{AccessPrefix}fallback:";
    private const string FallbackAccessIndexKey = $"{AccessPrefix}fallback-index";

    public AuditFallbackService(
        HybridCache cache,
        IAuditRequestRepository auditRequestRepository,
        IAuditChangeRepository auditChangeRepository)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _auditRequestRepository = auditRequestRepository ?? throw new ArgumentNullException(nameof(auditRequestRepository));
        _auditChangeRepository = auditChangeRepository ?? throw new ArgumentNullException(nameof(auditChangeRepository));
    }
    
    public async Task SaveFallbackSessionActivitiesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var currentBucketId = await _cache.GetOrCreateAsync(
            FallbackCurrentBucketIdKey,
            _ => ValueTask.FromResult(0L),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        var currentBucketCount = await _cache.GetOrCreateAsync(
            FallbackCurrentBucketCountKey,
            _ => ValueTask.FromResult(0),
            options: FallbackCacheOptions,
            cancellationToken: ct);
        
        long maxBucketToProcess;
        if (currentBucketCount > 0)
        {
            maxBucketToProcess = currentBucketId;
            await _cache.SetAsync(FallbackCurrentBucketIdKey, currentBucketId + 1, FallbackCacheOptions, cancellationToken: ct);
            await _cache.SetAsync(FallbackCurrentBucketCountKey, 0, FallbackCacheOptions, cancellationToken: ct);
        }
        else
        {
            maxBucketToProcess = currentBucketId - 1;
        }

        var drainCursor = await _cache.GetOrCreateAsync(
            FallbackDrainCursorKey,
            _ => ValueTask.FromResult(0L),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        if (maxBucketToProcess < drainCursor)
            return;

        var pendingActivityIds = new List<Guid>();
        var drainedBucketIds = new List<long>();

        for (var bucketId = drainCursor; bucketId <= maxBucketToProcess; bucketId++)
        {
            ct.ThrowIfCancellationRequested();

            var bucketActivityIds = await _cache.GetOrCreateAsync(
                GetFallbackBucketKey(bucketId),
                _ => ValueTask.FromResult(new List<Guid>()),
                options: FallbackCacheOptions,
                cancellationToken: ct);

            if (bucketActivityIds.Count == 0)
                continue;

            pendingActivityIds.AddRange(bucketActivityIds);
            drainedBucketIds.Add(bucketId);
        }

        if (pendingActivityIds.Count == 0)
        {
            await _cache.SetAsync(FallbackDrainCursorKey, maxBucketToProcess + 1, FallbackCacheOptions, cancellationToken: ct);
            return;
        }

        var failedBatches = new ConcurrentBag<int>();
        var batches = pendingActivityIds
            .Distinct()
            .Chunk(FallbackBatchSize)
            .Select(chunk => chunk.ToArray())
            .ToArray();

        await Parallel.ForEachAsync(batches.Select((ids, idx) => (ids, idx)), ct, async (entry, token) =>
        {
            try
            {
                await PersistFallbackBatchAsync(entry.ids, token);
            }
            catch
            {
                failedBatches.Add(entry.idx);
            }
        });

        if (!failedBatches.IsEmpty)
            return;

        foreach (var bucketId in drainedBucketIds)
        {
            await _cache.RemoveAsync(GetFallbackBucketKey(bucketId), ct);
        }

        await _cache.SetAsync(FallbackDrainCursorKey, maxBucketToProcess + 1, FallbackCacheOptions, cancellationToken: ct);
    }

    public async Task AddSessionActivityFallbackAsync(
        AuditRequest auditRequest, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditRequest);
        ct.ThrowIfCancellationRequested();

        var activityKey = GetFallbackActivityKey(auditRequest.Id);
        await _cache.SetAsync(activityKey, auditRequest, FallbackCacheOptions, cancellationToken: ct);

        var bucketId = await _cache.GetOrCreateAsync(
            FallbackCurrentBucketIdKey,
            _ => ValueTask.FromResult(0L),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        var bucketKey = GetFallbackBucketKey(bucketId);
        var bucket = await _cache.GetOrCreateAsync(
            bucketKey,
            _ => ValueTask.FromResult(new List<Guid>()),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        if (!bucket.Contains(auditRequest.Id))
        {
            bucket.Add(auditRequest.Id);
            await _cache.SetAsync(bucketKey, bucket, FallbackCacheOptions, cancellationToken: ct);
        }

        var bucketCount = await _cache.GetOrCreateAsync(
            FallbackCurrentBucketCountKey,
            _ => ValueTask.FromResult(0),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        bucketCount++;
        if (bucketCount >= FallbackBatchSize)
        {
            await _cache.SetAsync(FallbackCurrentBucketIdKey, bucketId + 1, FallbackCacheOptions, cancellationToken: ct);
            await _cache.SetAsync(FallbackCurrentBucketCountKey, 0, FallbackCacheOptions, cancellationToken: ct);
        }
        else
        {
            await _cache.SetAsync(FallbackCurrentBucketCountKey, bucketCount, FallbackCacheOptions, cancellationToken: ct);
        }
    }

    public async Task AddAuditChangeFallbackAsync(
        AuditChange auditChange,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditChange);
        ct.ThrowIfCancellationRequested();

        await _cache.SetAsync(
            GetFallbackChangeKey(auditChange.Id),
            auditChange,
            FallbackCacheOptions,
            cancellationToken: ct);

        var fallbackIndex = await _cache.GetOrCreateAsync(
            FallbackChangeIndexKey,
            _ => ValueTask.FromResult(new List<Guid>()),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        if (fallbackIndex.Contains(auditChange.Id))
            return;

        fallbackIndex.Add(auditChange.Id);

        await _cache.SetAsync(
            FallbackChangeIndexKey,
            fallbackIndex,
            FallbackCacheOptions,
            cancellationToken: ct);
    }

    public async Task AddAuditAccessFallbackAsync(
        AuditAccess auditAccess,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditAccess);
        ct.ThrowIfCancellationRequested();

        await _cache.SetAsync(
            GetFallbackAccessKey(auditAccess.Id),
            auditAccess,
            FallbackCacheOptions,
            cancellationToken: ct);

        var fallbackIndex = await _cache.GetOrCreateAsync(
            FallbackAccessIndexKey,
            _ => ValueTask.FromResult(new List<Guid>()),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        if (fallbackIndex.Contains(auditAccess.Id))
            return;

        fallbackIndex.Add(auditAccess.Id);

        await _cache.SetAsync(
            FallbackAccessIndexKey,
            fallbackIndex,
            FallbackCacheOptions,
            cancellationToken: ct);
    }

    public async Task SaveFallbackAuditChangesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var changeIds = await _cache.GetOrCreateAsync(
            FallbackChangeIndexKey,
            _ => ValueTask.FromResult(new List<Guid>()),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        if (changeIds.Count == 0)
            return;

        var changes = new List<AuditChange>(changeIds.Count);

        foreach (var changeId in changeIds.Distinct())
        {
            ct.ThrowIfCancellationRequested();

            var change = await _cache.GetOrCreateAsync<AuditChange?>(
                GetFallbackChangeKey(changeId),
                _ => ValueTask.FromResult<AuditChange?>(null),
                options: FallbackCacheOptions,
                cancellationToken: ct);

            if (change is null)
                continue;

            changes.Add(change);
        }

        if (changes.Count == 0)
            return;

        await _auditChangeRepository.Save(changes);

        foreach (var change in changes)
            await _cache.RemoveAsync(GetFallbackChangeKey(change.Id), ct);

        await _cache.RemoveAsync(FallbackChangeIndexKey, ct);
    }

    public async Task SaveFallbackAuditAccessesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var accessIds = await _cache.GetOrCreateAsync(
            FallbackAccessIndexKey,
            _ => ValueTask.FromResult(new List<Guid>()),
            options: FallbackCacheOptions,
            cancellationToken: ct);

        if (accessIds.Count == 0)
            return;

        var accesses = new List<AuditAccess>(accessIds.Count);

        foreach (var accessId in accessIds.Distinct())
        {
            ct.ThrowIfCancellationRequested();

            var access = await _cache.GetOrCreateAsync<AuditAccess?>(
                GetFallbackAccessKey(accessId),
                _ => ValueTask.FromResult<AuditAccess?>(null),
                options: FallbackCacheOptions,
                cancellationToken: ct);

            if (access is null)
                continue;

            accesses.Add(access);
        }

        if (accesses.Count == 0)
            return;

        foreach (var access in accesses)
            await _auditChangeRepository.Save(access);

        foreach (var access in accesses)
            await _cache.RemoveAsync(GetFallbackAccessKey(access.Id), ct);

        await _cache.RemoveAsync(FallbackAccessIndexKey, ct);
    }
    
    private async Task PersistFallbackBatchAsync(
        IReadOnlyCollection<Guid> activityIds,
        CancellationToken ct)
    {
        if (activityIds.Count == 0)
            return;

        var activities = new List<AuditRequest>(activityIds.Count);

        foreach (var activityId in activityIds)
        {
            ct.ThrowIfCancellationRequested();

            var activity = await _cache.GetOrCreateAsync<AuditRequest?>(
                GetFallbackActivityKey(activityId),
                _ => ValueTask.FromResult<AuditRequest?>(null),
                options: FallbackCacheOptions,
                cancellationToken: ct);

            if (activity is not null)
                activities.Add(activity);
        }

        if (activities.Count == 0)
            return;

        await _auditRequestRepository.SaveAsync(activities, ct);

        foreach (var activity in activities)
        {
            await _cache.RemoveAsync(GetFallbackActivityKey(activity.Id), ct);
        }
    }
    
    private static string GetFallbackActivityKey(Guid activityId) => $"{FallbackActivityKeyPrefix}{activityId:N}";
    private static string GetFallbackBucketKey(long bucketId) => $"{FallbackBucketPrefix}{bucketId}";
    private static string GetFallbackChangeKey(Guid changeId) => $"{FallbackChangeKeyPrefix}{changeId:N}";
    private static string GetFallbackAccessKey(Guid accessId) => $"{FallbackAccessKeyPrefix}{accessId:N}";
    private static string GetActivitiesKey(Guid sessionId) => $"{RequestPrefix}activities:{sessionId:N}";
    
    private static readonly HybridCacheEntryOptions FallbackCacheOptions = new()
    {
        
        Expiration = TimeSpan.FromDays(3650),
        LocalCacheExpiration = TimeSpan.FromDays(3650)
    };
    
    private async Task<List<AuditRequest>> GetActivitiesAsync(Guid sessionId, CancellationToken ct)
    {
        var activities = await _cache.GetOrCreateAsync(
            GetActivitiesKey(sessionId),
            _ => ValueTask.FromResult(new List<AuditRequest>()),
            cancellationToken: ct);

        return activities;
    }
    
}