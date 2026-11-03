using FieldService.AccountManagement.Dtos;
using FieldService.AccountManagement.Interfaces;
using FieldService.Shared.Types;
using FieldService.Shared.Types.Http;
using Microsoft.Extensions.Caching.Hybrid;

namespace FieldService.AccountManagement.Services;

/// <summary>
/// Cache-aside sobre o <see cref="IAccountManagementRepository"/> usando HybridCache.GetOrCreate.
/// O repository é a factory; resultados null nunca permanecem em cache.
/// Todas as entradas de um tenant recebem a tag do tenant para invalidação em bloco.
/// </summary>
internal sealed class AccountManagementService(
    HybridCache cache,
    IAccountManagementRepository repository) : IAccountManagementService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = CacheTtl,
        LocalCacheExpiration = CacheTtl
    };

    /// <summary>
    /// Invalida entidades, listas e projeções relacionadas do tenant.
    /// </summary>
    public Task RemoveCache(Guid tenantId, Guid? entityId, CancellationToken cancellationToken)
    {
        return cache.RemoveByTagAsync(TenantTag(tenantId), cancellationToken).AsTask();
    }

    public Task<TenantDto?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(
            TenantKey(tenantId),
            tenantId,
            async ct =>
            {
                var tenant = await repository.GetTenantByIdAsync(tenantId, ct);
                if (tenant is null)
                    return null;

                var dto = tenant.ToTenantDto();
                return dto with
                {
                    JobTitles = dto.JobTitles.ToList(),
                    Teams = dto.Teams.ToList()
                };
            },
            cancellationToken);
    }

    public Task<TenantReferenceDto?> GetTenantReferenceByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(
            TenantReferenceKey(tenantId),
            tenantId,
            async ct =>
            {
                var reference = await repository.GetTenantReferenceByIdAsync(tenantId, ct);
                return reference is null ? null : new TenantReferenceDto(reference.TenantId, reference.Name);
            },
            cancellationToken);
    }

    public async Task<IEnumerable<TenantReferenceDto>> GetTenantReferencesByTenantIdsAsync(
        IEnumerable<Guid> tenantIds,
        CancellationToken cancellationToken)
    {
        var ids = tenantIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        if (ids.Length == 0)
            return [];

        var cacheKey = TenantReferencesByIdsKey(ids);
        var tags = ids.Select(TenantTag).ToArray();

        return await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var refs = await repository.GetTenantReferencesAsync(ids, ct);
                return refs
                    .Select(r => new TenantReferenceDto(r.TenantId, r.Name))
                    .ToList();
            },
            CacheOptions,
            tags,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<TeamDto>> GetTeamsByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await GetOrCreateListAsync(
            TeamsKey(tenantId),
            tenantId,
            async ct => (await repository.GetTeamsByTenantIdAsync(tenantId, ct)).Select(t => t.ToDto()).ToList(),
            cancellationToken);
    }

    public Task<TeamDto?> GetTeamByIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(
            TeamKey(tenantId, teamId),
            tenantId,
            async ct => (await repository.GetTeamByIdAsync(tenantId, teamId, ct))?.ToDto(),
            cancellationToken);
    }

    public Task<UserAccountDto?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(
            UserKey(tenantId, userId),
            tenantId,
            async ct => (await repository.GetUserByIdAsync(tenantId, userId, ct))?.ToDto(),
            cancellationToken);
    }

    public async Task<IEnumerable<UserAccountDto>> GetUsersByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await GetOrCreateListAsync(
            UsersKey(tenantId),
            tenantId,
            async ct => (await repository.GetUsersByTenantIdAsync(tenantId, ct)).Select(u => u.ToDto()).ToList(),
            cancellationToken);
    }

    public async Task<IEnumerable<UserAccountDto>> GetUsersByTeamIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken)
    {
        return await GetOrCreateListAsync(
            UsersByTeamKey(tenantId, teamId),
            tenantId,
            async ct => (await repository.GetUsersByTeamIdAsync(tenantId, teamId, ct)).Select(u => u.ToDto()).ToList(),
            cancellationToken);
    }

    public async Task<PaginatedResult<UserAccountDto>> GetUsersByTenantIdAsync(
        Guid tenantId, PagedRequest pagedRequest, CancellationToken cancellationToken)
    {
        var request = NormalizePage(pagedRequest);
        return await cache.GetOrCreateAsync(
            UsersPageKey(tenantId, null, request),
            async ct =>
            {
                var result = await repository.GetUsersByTenantIdAsync(tenantId, request, ct);
                return new PaginatedResult<UserAccountDto>(
                    result.Pagination, result.Data.Select(user => user.ToDto()).ToList());
            },
            CacheOptions, [TenantTag(tenantId)], cancellationToken);
    }

    public async Task<PaginatedResult<UserAccountDto>> GetUsersByTeamIdAsync(
        Guid tenantId, Guid teamId, PagedRequest pagedRequest, CancellationToken cancellationToken)
    {
        var request = NormalizePage(pagedRequest);
        return await cache.GetOrCreateAsync(
            UsersPageKey(tenantId, teamId, request),
            async ct =>
            {
                var result = await repository.GetUsersByTeamIdAsync(tenantId, teamId, request, ct);
                return new PaginatedResult<UserAccountDto>(
                    result.Pagination, result.Data.Select(user => user.ToDto()).ToList());
            },
            CacheOptions, [TenantTag(tenantId)], cancellationToken);
    }

    public async Task<IEnumerable<JobTitleDto>> GetJobTitlesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await GetOrCreateListAsync(
            JobTitlesKey(tenantId),
            tenantId,
            async ct => (await repository.GetJobTitlesByTenantIdAsync(tenantId, ct)).Select(j => j.ToDto()).ToList(),
            cancellationToken);
    }

    public Task<JobTitleDto?> GetJobTitleByIdAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(
            JobTitleKey(tenantId, jobTitleId),
            tenantId,
            async ct => (await repository.GetJobTitleByIdAsync(tenantId, jobTitleId, ct))?.ToDto(),
            cancellationToken);
    }

    /// <summary>
    /// GetOrCreate que não mantém null em cache: se a factory devolver null, a entrada é removida.
    /// </summary>
    private async Task<T?> GetOrCreateAsync<T>(
        string key,
        Guid tenantId,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken)
        where T : class
    {
        var result = await cache.GetOrCreateAsync<T?>(
            key,
            async ct => await factory(ct),
            CacheOptions,
            [TenantTag(tenantId)],
            cancellationToken);

        if (result is null)
            await cache.RemoveAsync(key, cancellationToken);

        return result;
    }

    private async Task<IEnumerable<T>> GetOrCreateListAsync<T>(
        string key,
        Guid tenantId,
        Func<CancellationToken, Task<List<T>>> factory,
        CancellationToken cancellationToken)
    {
        var result = await GetOrCreateAsync<List<T>>(key, tenantId, async ct => await factory(ct), cancellationToken);
        return result ?? [];
    }

    private static string TenantTag(Guid tenantId) => $"account-management:tenant:{tenantId:N}";

    private static PagedRequest NormalizePage(PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new PagedRequest(request.Page, request.PageSize, request.SortBy, request.IsDescending);
    }

    private static string UsersPageKey(Guid tenantId, Guid? teamId, PagedRequest request) =>
        $"{Prefix(tenantId)}users-page:{teamId?.ToString("N") ?? "all"}:{request.Page}:{request.PageSize}:{request.SortBy?.Trim().ToLowerInvariant()}:{request.IsDescending}";

    private static string Prefix(Guid tenantId) => $"account-management:v2:{tenantId:N}:";
    private static string TenantKey(Guid tenantId) => $"{Prefix(tenantId)}tenant";
    private static string TenantReferenceKey(Guid tenantId) => $"{Prefix(tenantId)}tenant-reference";
    private static string TenantReferencesByIdsKey(IEnumerable<Guid> tenantIds) =>
        $"account-management:tenant-references:{string.Join('-', tenantIds.Select(id => id.ToString("N")))}";
    private static string TeamsKey(Guid tenantId) => $"{Prefix(tenantId)}teams";
    private static string TeamKey(Guid tenantId, Guid teamId) => $"{Prefix(tenantId)}team:{teamId:N}";
    private static string UsersKey(Guid tenantId) => $"{Prefix(tenantId)}users";
    private static string UserKey(Guid tenantId, Guid userId) => $"{Prefix(tenantId)}user:{userId:N}";
    private static string UsersByTeamKey(Guid tenantId, Guid teamId) => $"{Prefix(tenantId)}users-by-team:{teamId:N}";
    private static string JobTitlesKey(Guid tenantId) => $"{Prefix(tenantId)}job-titles";
    private static string JobTitleKey(Guid tenantId, Guid jobTitleId) => $"{Prefix(tenantId)}job-title:{jobTitleId:N}";
}
