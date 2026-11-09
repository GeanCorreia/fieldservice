using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Events;
using FieldService.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace FieldService.InfraTest.AccountManagement;

public sealed class TenantReferencesCacheTests
{
    [Fact]
    public async Task Missing_tenants_are_not_cached_and_are_loaded_when_created()
    {
        await using var fixture = new Fixture();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await fixture.AddTenant(firstId, "First");

        Assert.Single(await fixture.GetReferences(firstId, secondId));
        Assert.Null(await fixture.GetCachedReferences(firstId, secondId));
        Assert.False(await fixture.HasCacheEntry(firstId, secondId));

        await fixture.AddTenant(secondId, "Second");
        var references = (await fixture.GetReferences(firstId, secondId)).ToList();
        Assert.Equal(2, references.Count);
        Assert.Contains(references, tenant => tenant.Id == firstId && tenant.Name == "First");
        Assert.Contains(references, tenant => tenant.Id == secondId && tenant.Name == "Second");
        Assert.NotNull(await fixture.GetCachedReferences(firstId, secondId));
        await fixture.RemoveTenantEvents();
        Assert.Equal(2, (await fixture.GetReferences(firstId, secondId)).Count());
    }

    [Fact]
    public async Task Invalid_cached_results_are_reloaded_and_complete_results_are_invalidated_by_tag()
    {
        await using var fixture = new Fixture();
        var id = Guid.NewGuid();
        await fixture.AddTenant(id, "Actual name");
        await fixture.SetCachedReferences(id, [new TenantReferenceDto(id, "")]);

        Assert.Equal("Actual name", Assert.Single(await fixture.GetReferences(id)).Name);
        Assert.Equal("Actual name", Assert.Single((await fixture.GetCachedReferences(id))!).Name);

        await fixture.Cache.RemoveByTagAsync($"account-management:tenant:{id:N}");
        Assert.Null(await fixture.GetCachedReferences(id));
    }

    [Fact]
    public async Task Cached_null_is_reloaded_from_repository()
    {
        await using var fixture = new Fixture();
        var id = Guid.NewGuid();
        await fixture.AddTenant(id, "Actual name");
        await fixture.SetCachedReferences(id, null);

        Assert.Equal("Actual name", Assert.Single(await fixture.GetReferences(id)).Name);
    }

    [Fact]
    public async Task Cached_empty_list_is_reloaded_from_repository()
    {
        await using var fixture = new Fixture();
        var id = Guid.NewGuid();
        await fixture.AddTenant(id, "Actual name");
        await fixture.SetCachedReferences(id, []);

        Assert.Equal("Actual name", Assert.Single(await fixture.GetReferences(id)).Name);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly AccountManagementDbContext _db;
        private readonly object _service;

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddFusionCache().AsHybridCache();
            _provider = services.BuildServiceProvider();
            Cache = _provider.GetRequiredService<HybridCache>();
            _db = new AccountManagementDbContext(
                new DbContextOptionsBuilder<AccountManagementDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            var assembly = typeof(AccountManagementDbContext).Assembly;
            var repositoryType = assembly.GetType("FieldService.AccountManagement.Data.Repositories.AccountManagementRepository", true)!;
            var repository = Activator.CreateInstance(repositoryType, _db, null)!;
            var serviceType = assembly.GetType("FieldService.AccountManagement.Services.AccountManagementService", true)!;
            _service = Activator.CreateInstance(serviceType, Cache, repository)!;
        }

        public HybridCache Cache { get; }

        public async Task AddTenant(Guid id, string name)
        {
            _db.TenantEvents.Add(TenantCreatedEvent.Create(id, name, Guid.NewGuid()));
            await _db.SaveChangesAsync();
        }

        public async Task RemoveTenantEvents()
        {
            _db.TenantEvents.RemoveRange(_db.TenantEvents);
            await _db.SaveChangesAsync();
        }

        public Task<IEnumerable<TenantReferenceDto>> GetReferences(params Guid[] ids) =>
            (Task<IEnumerable<TenantReferenceDto>>)_service.GetType()
                .GetMethod("GetTenantReferencesByTenantIdsAsync")!
                .Invoke(_service, [ids, CancellationToken.None])!;

        public ValueTask<List<TenantReferenceDto>?> GetCachedReferences(params Guid[] ids) =>
            Cache.GetOrCreateAsync<List<TenantReferenceDto>?>(
                Key(ids),
                _ => ValueTask.FromResult<List<TenantReferenceDto>?>(null),
                new HybridCacheEntryOptions
                {
                    Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
                });

        public ValueTask SetCachedReferences(Guid id, List<TenantReferenceDto>? references) =>
            Cache.SetAsync(Key([id]), references);

        public async Task<bool> HasCacheEntry(params Guid[] ids) =>
            (await _provider.GetRequiredService<IFusionCache>()
                .TryGetAsync<List<TenantReferenceDto>?>(Key(ids))).HasValue;

        private static string Key(IEnumerable<Guid> ids) =>
            $"account-management:tenant-references:{string.Join('-', ids.OrderBy(id => id).Select(id => id.ToString("N")))}";

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }
}
