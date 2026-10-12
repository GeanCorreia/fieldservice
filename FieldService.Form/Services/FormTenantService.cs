using FieldService.Form.Data.Repositories;
using FieldService.Form.Entities;
using FieldService.Form.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace FieldService.Form.Services;

internal sealed class FormTenantService(
    HybridCache cache,
    FormTenantRepository repository) : IFormTenantService
{
    private const string FormTenantPrefix = "form-tenant:";
    private const string ByIdPrefix = FormTenantPrefix + "by-id:";
    private const string ByTenantIdPrefix = FormTenantPrefix + "by-tenant-id:";

    private readonly HybridCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly FormTenantRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    private HybridCacheEntryOptions CacheOptions => new()
    {
        Expiration = _cacheTtl,
        LocalCacheExpiration = _cacheTtl
    };

    public async Task<FormTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == default)
            throw new ArgumentException("Id is required.", nameof(id));

        return await _cache.GetOrCreateAsync(
            GetByIdKey(id),
            ct => new ValueTask<FormTenant?>(_repository.GetByIdAsync(id, ct)),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<FormTenant?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        return await _cache.GetOrCreateAsync(
            GetByTenantIdKey(tenantId),
            ct => new ValueTask<FormTenant?>(_repository.GetByTenantIdAsync(tenantId, ct)),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task SaveAsync(FormTenant formTenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formTenant);

        await _repository.SaveAsync(formTenant, cancellationToken);

        await _cache.RemoveAsync(GetByIdKey(formTenant.Id), cancellationToken);
        await _cache.RemoveAsync(GetByTenantIdKey(formTenant.TenantId), cancellationToken);
    }

    private static string GetByIdKey(Guid id) => $"{ByIdPrefix}{id:N}";
    private static string GetByTenantIdKey(Guid tenantId) => $"{ByTenantIdPrefix}{tenantId:N}";
}

