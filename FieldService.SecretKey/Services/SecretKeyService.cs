using FieldService.SecretKey.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace FieldService.SecretKey.Services;

internal sealed class SecretKeyService(
    ISecretKeyRepository secretKeyRepository,
    HybridCache hybridCache) : ISecretKeyService
{
    private readonly ISecretKeyRepository _secretKeyRepository = secretKeyRepository ?? throw new ArgumentNullException(nameof(secretKeyRepository));
    private readonly HybridCache _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(5),
        Flags = HybridCacheEntryFlags.DisableDistributedCache
    };

    private static string GetByIdKey(Guid secretKeyId) => $"secret-key:id:{secretKeyId:N}";

    private static string GetByNameKey(Guid tenantId, string name)
        => $"secret-key:tenant:{tenantId:N}:name:{Uri.EscapeDataString(Entities.SecretKey.SanitizeName(name))}";

    public async Task<Entities.SecretKey?> GetSecretKeyAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return await _hybridCache.GetOrCreateAsync(
            GetByNameKey(tenantId, name),
            async token => await _secretKeyRepository.GetSecretKeyAsync(tenantId, name, token),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<Entities.SecretKey?> GetSecretKeyAsync(
        Guid secretKeyId,
        CancellationToken cancellationToken = default)
    {
        if (secretKeyId == default)
            throw new ArgumentException("SecretKeyId is required.", nameof(secretKeyId));

        return await _hybridCache.GetOrCreateAsync(
            GetByIdKey(secretKeyId),
            async token => await _secretKeyRepository.GetSecretKeyAsync(secretKeyId, token),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task SaveSecretKeyAsync(
        Entities.SecretKey secretKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secretKey);

        var existing = await _secretKeyRepository.GetSecretKeyAsync(secretKey.Id, cancellationToken);

        await _secretKeyRepository.SaveSecretKeyAsync(secretKey, cancellationToken);

        await _hybridCache.SetAsync(GetByIdKey(secretKey.Id), secretKey, CacheOptions, cancellationToken: cancellationToken);

        var currentNameKey = GetByNameKey(secretKey.TenantId, secretKey.Reference.Name);
        if (secretKey.IsDeleted)
        {
            await _hybridCache.RemoveAsync(currentNameKey, cancellationToken);
        }
        else
        {
            await _hybridCache.SetAsync(currentNameKey, secretKey, CacheOptions, cancellationToken: cancellationToken);
        }

        if (existing is not null)
        {
            var existingNameKey = GetByNameKey(existing.TenantId, existing.Reference.Name);
            if (!string.Equals(existingNameKey, currentNameKey, StringComparison.Ordinal))
            {
                await _hybridCache.RemoveAsync(existingNameKey, cancellationToken);
            }
        }
    }

    public async Task RemoveSecretKeyAsync(
        Guid secretKeyId,
        CancellationToken cancellationToken = default)
    {
        if (secretKeyId == default)
            throw new ArgumentException("SecretKeyId is required.", nameof(secretKeyId));

        var existing = await GetSecretKeyAsync(secretKeyId, cancellationToken);
        if (existing is null)
            return;

        await _hybridCache.RemoveAsync(GetByIdKey(secretKeyId), cancellationToken);
        await _hybridCache.RemoveAsync(GetByNameKey(existing.TenantId, existing.Reference.Name), cancellationToken);
    }
}

