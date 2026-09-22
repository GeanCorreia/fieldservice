using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using FieldService.SecretKey.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace FieldService.SecretKey.Services;

internal sealed class SecretKeyVaultService : ISecretKeyVaultService
{
    private readonly HybridCache _cache;
    private readonly SecretClient _secretClient;
    private readonly IMemoryCache _localLockCache;

    private const string CacheKeyPrefix = "secret:";
    private const string LockKeyPrefix = "lock-eviction:";
    
    private static readonly HybridCacheEntryOptions DefaultCacheOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromHours(12),
        Flags = HybridCacheEntryFlags.DisableDistributedCache
    };

    public SecretKeyVaultService(
        HybridCache cache, 
        SecretClient secretClient, 
        IMemoryCache localLockCache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _localLockCache = localLockCache ?? throw new ArgumentNullException(nameof(localLockCache));
    }

    public async Task<T?> GetSecretKeyAsync<T>(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
        where T : ISecretKeyType
    {
        string secretName = BuildSecretName(tenantId, name);
        string cacheKey = $"{CacheKeyPrefix}{secretName}";
        
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                try
                {
                    KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancel);

                    if (string.IsNullOrWhiteSpace(secret.Value))
                        return default;

                    return JsonSerializer.Deserialize<T>(secret.Value);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    return default;
                }
            },
            options: DefaultCacheOptions,
            cancellationToken: cancellationToken
        );
    }

    public async Task SetSecretKeyAsync<T>(
        Guid tenantId,
        string name,
        T secretKey,
        CancellationToken cancellationToken = default)
        where T : ISecretKeyType
    {
        string secretName = BuildSecretName(tenantId, name);
        string cacheKey = $"{CacheKeyPrefix}{secretName}";
        string lockKey = $"{LockKeyPrefix}{secretName}";
        string jsonValue = JsonSerializer.Serialize(secretKey);
        
        _localLockCache.Set(lockKey, true, TimeSpan.FromMinutes(1));

        await _secretClient.SetSecretAsync(secretName, jsonValue, cancellationToken);
        
        await _cache.SetAsync(
            cacheKey, 
            secretKey, 
            options: DefaultCacheOptions, 
            cancellationToken: cancellationToken);
    }

    public async Task RemoveSecretKeyAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        string secretName = BuildSecretName(tenantId, name);
        string cacheKey = $"{CacheKeyPrefix}{secretName}";

        try
        {
            var operation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }

        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    public async Task EvictFromServiceBusAsync(string secretName, CancellationToken cancellationToken = default)
    {
        string lockKey = $"{LockKeyPrefix}{secretName}";
        if (_localLockCache.TryGetValue(lockKey, out _))
        {
            return;
        }

        string cacheKey = $"{CacheKeyPrefix}{secretName}";
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private static string BuildSecretName(Guid tenantId, string name)
    {
        if (name.StartsWith($"tenant-{tenantId:N}-", StringComparison.OrdinalIgnoreCase))
            return name;

        return $"tenant-{tenantId:N}-{name}";
    }
}