using FieldService.Cloud;
using FieldService.Cloud.Interfaces;
using FieldService.Cloud.Services;
using FieldService.Superset;
using Medallion.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FieldService.InfraTest.Cloud;

public class ContainerProcessingLockTests
{
    [Fact]
    public async Task AcquireAndReleaseUseTheSameKeyAndDisposeTheHandle()
    {
        var provider = new TestLockProvider();
        var processingLock = CreateService(provider);
        var timeout = TimeSpan.FromMinutes(5);

        Assert.True(await processingLock.AcquireAsync("SupersetTenantInstance:tenant", timeout));
        Assert.Equal("SupersetTenantInstance:tenant", provider.LastKey);
        Assert.Equal(timeout, provider.Lock.Timeout);
        Assert.True(await processingLock.ReleaseAsync("SupersetTenantInstance:tenant"));
        Assert.True(Assert.IsType<TestHandle>(provider.Handle).Disposed);
        Assert.False(await processingLock.ReleaseAsync("SupersetTenantInstance:tenant"));
    }

    [Fact]
    public async Task DifferentContainerTypesHaveIndependentLocks()
    {
        var provider = new TestLockProvider();
        var processingLock = CreateService(provider);
        var supersetHandle = Assert.IsType<TestHandle>(provider.Handle);
        Assert.True(await processingLock.AcquireAsync("SupersetTenantInstance:tenant", TimeSpan.Zero));
        provider.Handle = new TestHandle();
        var formHandle = provider.Handle;
        Assert.True(await processingLock.AcquireAsync("FormTenantInstance:tenant", TimeSpan.Zero));

        Assert.True(await processingLock.ReleaseAsync("SupersetTenantInstance:tenant"));
        Assert.True(supersetHandle.Disposed);
        Assert.False(formHandle.Disposed);
        Assert.True(await processingLock.ReleaseAsync("FormTenantInstance:tenant"));
        Assert.True(formHandle.Disposed);
    }

    [Fact]
    public async Task FailedAcquisitionDoesNotStoreAHandle()
    {
        var provider = new TestLockProvider { Handle = null };
        var processingLock = CreateService(provider);

        Assert.False(await processingLock.AcquireAsync("container", TimeSpan.Zero));
        Assert.False(await processingLock.ReleaseAsync("container"));
    }

    [Fact]
    public async Task DuplicateAcquisitionDoesNotOverwriteAnOwnedHandle()
    {
        var provider = new TestLockProvider();
        var processingLock = CreateService(provider);
        var firstHandle = Assert.IsType<TestHandle>(provider.Handle);
        Assert.True(await processingLock.AcquireAsync("container", TimeSpan.Zero));
        provider.Handle = new TestHandle();
        var secondHandle = provider.Handle;

        Assert.False(await processingLock.AcquireAsync("container", TimeSpan.Zero));
        Assert.True(secondHandle.Disposed);
        Assert.False(firstHandle.Disposed);
        Assert.True(await processingLock.ReleaseAsync("container"));
        Assert.True(firstHandle.Disposed);
    }

    [Fact]
    public async Task CancellationCanStillBeFollowedByUncancelledCleanup()
    {
        var provider = new TestLockProvider();
        var processingLock = CreateService(provider);
        Assert.True(await processingLock.AcquireAsync("container", TimeSpan.Zero));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => processingLock.ReleaseAsync("container", cts.Token));
        Assert.False(Assert.IsType<TestHandle>(provider.Handle).Disposed);
        Assert.True(await processingLock.ReleaseAsync("container", CancellationToken.None));
        Assert.True(Assert.IsType<TestHandle>(provider.Handle).Disposed);
    }

    [Fact]
    public async Task SupersetAdapterSharesCloudLockAcrossScopes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=test",
            ["Superset:RouteId"] = "superset",
            ["Superset:ClusterId"] = "superset",
            ["Superset:DestinationName"] = "default",
            ["Superset:RoutePath"] = "/superset/{**catch-all}",
            ["Superset:BaseUrl"] = "https://example.com",
            ["Superset:LockTtlMinutes"] = "7"
        }).Build();
        var provider = new TestLockProvider();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDistributedLockProvider>(provider);
        services.AddCloudModule(configuration);
        services.AddSupersetModule(configuration);
        var adapter = Assert.Single(services,
            descriptor => descriptor.ServiceType.Name == "ISupersetTenantInstanceProcessingLock");
        Assert.Equal(ServiceLifetime.Singleton, adapter.Lifetime);
        using var serviceProvider = services.BuildServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();
        var firstAdapter = firstScope.ServiceProvider.GetRequiredService(adapter.ServiceType);
        var secondAdapter = secondScope.ServiceProvider.GetRequiredService(adapter.ServiceType);
        Assert.Same(firstAdapter, secondAdapter);
        var firstLock = firstScope.ServiceProvider.GetRequiredService<IContainerProcessingLock>();
        var secondLock = secondScope.ServiceProvider.GetRequiredService<IContainerProcessingLock>();

        Assert.Same(firstLock, secondLock);
        Assert.NotNull(serviceProvider.GetRequiredService<IAzureContainerService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IAzureContainerConfigurationService>());
        var tenantId = Guid.NewGuid();
        var acquire = adapter.ServiceType.GetMethod("AcquireLock")!;
        var release = adapter.ServiceType.GetMethod("ReleaseLock")!;
        Assert.True(await Assert.IsAssignableFrom<Task<bool>>(
            acquire.Invoke(firstAdapter, [tenantId, CancellationToken.None])));
        Assert.Equal($"SupersetTenantInstance:{tenantId}", provider.LastKey);
        Assert.Equal(TimeSpan.FromMinutes(7), provider.Lock.Timeout);
        Assert.True(await Assert.IsAssignableFrom<Task<bool>>(
            release.Invoke(secondAdapter, [tenantId, CancellationToken.None])));
        Assert.True(Assert.IsType<TestHandle>(provider.Handle).Disposed);
    }

    private static ContainerProcessingLock CreateService(TestLockProvider provider) =>
        new(provider, NullLogger<ContainerProcessingLock>.Instance);

    private sealed class TestHandle : IDistributedSynchronizationHandle
    {
        public CancellationToken HandleLostToken => CancellationToken.None;
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestLockProvider : IDistributedLockProvider
    {
        public TestHandle? Handle { get; set; } = new();
        public string? LastKey { get; private set; }
        public TestLock Lock { get; private set; } = null!;
        public IDistributedLock CreateLock(string name)
        {
            LastKey = name;
            Lock = new TestLock(name, Handle);
            return Lock;
        }
    }

    private sealed class TestLock(string name, TestHandle? handle) : IDistributedLock
    {
        public string Name => name;
        public TimeSpan Timeout { get; private set; }
        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Timeout = timeout;
            return handle;
        }
        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            TryAcquire(timeout ?? TimeSpan.Zero, cancellationToken) ?? throw new TimeoutException();
        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(
            TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(TryAcquire(timeout, cancellationToken));
        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(
            TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Acquire(timeout, cancellationToken));
    }
}
