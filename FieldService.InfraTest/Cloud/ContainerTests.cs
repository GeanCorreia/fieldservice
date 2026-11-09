using FieldService.Cloud;
using FieldService.Cloud.Data;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;
using FieldService.Data.Interfaces;
using FieldService.Shared.Types;
using FieldService.Superset.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace FieldService.InfraTest.Cloud;

public class ContainerTests
{
    [Fact]
    public void NameIsSuppliedByTheApplication()
    {
        var container = CreateContainer(Guid.NewGuid(), "another_application");

        Assert.Equal("another_application", container.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void EmptyResourceNamesAreRejected(string? resourceName)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateContainer(Guid.NewGuid(), resourceName!));
    }

    [Fact]
    public void ConfigurationCanSwitchToScheduledAndBack()
    {
        var container = CreateContainer(Guid.NewGuid(), "test");
        var window = new ExecutionWindow(
            new DailyExecutionWindow(new TimeOnly(8, 0), new TimeOnly(18, 0)),
            null, null, null, null, null, null);
        var configuration = new ContainerConfiguration(ExecutionType.Scheduled, 3, 1, window);

        container.UpdateConfiguration(configuration);

        Assert.Equal(configuration, container.Configuration);
        Assert.Equal(window, container.ExecutionWindow);

        container.UpdateConfiguration(new ContainerConfiguration(ExecutionType.OnDemand, 1, 0));

        Assert.Null(container.ExecutionWindow);
        Assert.Equal(ExecutionType.OnDemand, container.ExecutionType);
        Assert.Equal(0, container.MinReplicas);
        Assert.Equal(1, container.MaxReplicas);
    }

    [Fact]
    public void ScheduledContainerRequiresExecutionWindow()
    {
        Assert.Throws<ArgumentException>(() => CreateContainer(Guid.NewGuid(), "test", ExecutionType.Scheduled));
    }

    [Fact]
    public void ProviderInformationCanBeUpdated()
    {
        var container = CreateContainer(Guid.NewGuid(), "test");

        container.UpdateProviderInfo(container.CloudProvider, "new-resource", "https://new.example.com");

        Assert.Equal("new-resource", container.ResourceId);
        Assert.Equal("https://new.example.com", container.FqdnUrl);
    }

    [Fact]
    public void BothContextsMapTheSameConcreteEntityButOnlyCloudOwnsTheTable()
    {
        using var cloud = new CloudDbContext(new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        using var superset = new SupersetDbContext(new DbContextOptionsBuilder<SupersetDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);

        foreach (var context in new DbContext[] { cloud, superset })
        {
            var entity = context.Model.FindEntityType(typeof(Container))!;
            var designEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Container))!;

            Assert.Equal("Container", entity.GetTableName());
            Assert.Equal(context == superset, designEntity.IsTableExcludedFromMigrations());
            Assert.Null(entity.BaseType);
            Assert.NotNull(entity.FindProperty(nameof(Container.Name))!.PropertyInfo);
            Assert.NotNull(entity.FindProperty("_executionWindow")!.FieldInfo);
            Assert.Null(entity.FindProperty(nameof(Container.Configuration)));
            Assert.Null(entity.FindProperty(nameof(Container.ExecutionWindow)));
        }

        var tenant = superset.Model.FindEntityType("FieldService.Superset.Entities.SupersetTenant")!;
        Assert.Equal(typeof(Container), tenant.FindNavigation("Container")!.TargetEntityType.ClrType);
    }

    [Fact]
    public async Task CloudRepositoryFindsContainersBeforeTenantCreationAndSeparatesWorkloads()
    {
        using var cloud = new CloudDbContext(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=test"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCloudModule(configuration);
        services.AddScoped(_ => cloud);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var tenantId = Guid.NewGuid();
        var first = CreateContainer(tenantId, "superset");
        var second = CreateContainer(tenantId, "another_app");

        await repository.SaveAsync(first);
        await repository.SaveAsync(second);
        cloud.ChangeTracker.Clear();

        Assert.Equal(first.Id, (await repository.GetByTenantAndNameAsync(tenantId, first.Name))!.Id);
        Assert.Equal(second.Id, (await repository.GetByTenantAndNameAsync(tenantId, second.Name))!.Id);
        Assert.Null(await repository.GetByTenantAndNameAsync(Guid.NewGuid(), first.Name));
        var detached = (await repository.GetByIdAsync(first.Id))!;
        detached.UpdateProviderInfo(detached.CloudProvider, "updated-resource", "https://updated.example.com");
        var window = new ExecutionWindow(
            new DailyExecutionWindow(new TimeOnly(8, 0), new TimeOnly(18, 0)),
            null, null, null, null, null, null);
        detached.UpdateConfiguration(new ContainerConfiguration(ExecutionType.Scheduled, 3, 1, window));
        await repository.SaveAsync(detached);
        cloud.ChangeTracker.Clear();
        var updated = (await repository.GetByIdAsync(first.Id))!;
        Assert.Equal("updated-resource", updated.ResourceId);
        Assert.Equal(window, updated.ExecutionWindow);
    }

    [Fact]
    public async Task SupersetRepositoryPreservesNavigationWithoutWritingContainerChanges()
    {
        var root = new InMemoryDatabaseRoot();
        var database = Guid.NewGuid().ToString();
        using var cloud = new CloudDbContext(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(database, root).Options);
        using var superset = new SupersetDbContext(new DbContextOptionsBuilder<SupersetDbContext>()
            .UseInMemoryDatabase(database, root).Options);
        var container = CreateContainer(Guid.NewGuid(), "test");
        cloud.Containers.Add(container);
        await cloud.SaveChangesAsync();
        cloud.ChangeTracker.Clear();

        var tenantType = typeof(SupersetDbContext).Assembly.GetType("FieldService.Superset.Entities.SupersetTenant")!;
        var tenant = tenantType.GetMethod("Create")!.Invoke(null, [container.TenantId, container, Guid.NewGuid()])!;
        var repositoryType = typeof(SupersetDbContext).Assembly
            .GetType("FieldService.Superset.Data.Repositories.SupersetRepository")!;
        var repository = Activator.CreateInstance(repositoryType, superset, new SavingUnitOfWork(superset))!;

        container.UpdateProviderInfo(container.CloudProvider, "must-not-be-saved", "https://changed.example.com");
        await (Task)repositoryType.GetMethod("SaveAsync")!.Invoke(repository, [tenant, CancellationToken.None])!;
        superset.ChangeTracker.Clear();

        var loaded = (await LoadTenant(repositoryType, repository, container.TenantId))!;
        var navigation = (Container)tenantType.GetProperty("Container")!.GetValue(loaded)!;
        Assert.Equal(container.Id, navigation.Id);
        Assert.NotEqual("must-not-be-saved", navigation.ResourceId);
        Assert.Equal(navigation.ResourceId, (await cloud.Containers.SingleAsync()).ResourceId);

        var resourceQuery = (Task)repositoryType.GetMethod("GetSupersetTenantByResourceIdAsync")!
            .Invoke(repository, [navigation.ResourceId, CancellationToken.None])!;
        await resourceQuery;
        Assert.NotNull(resourceQuery.GetType().GetProperty("Result")!.GetValue(resourceQuery));

        tenantType.GetMethod("MarkAsSuspended")!.Invoke(loaded, null);
        navigation.UpdateProviderInfo(navigation.CloudProvider, "still-must-not-be-saved", "https://changed.example.com");
        await (Task)repositoryType.GetMethod("SaveAsync")!.Invoke(repository, [loaded, CancellationToken.None])!;
        superset.ChangeTracker.Clear();
        var savedTenant = (await LoadTenant(repositoryType, repository, container.TenantId))!;
        var savedNavigation = (Container)tenantType.GetProperty("Container")!.GetValue(savedTenant)!;
        Assert.NotEqual("still-must-not-be-saved", savedNavigation.ResourceId);
        Assert.Equal("Suspended", tenantType.GetProperty("Status")!.GetValue(savedTenant)!.ToString());
    }

    [Fact]
    public void OwnershipMigrationCopiesLegacyDataBeforeReplacingItsTableAndForeignKey()
    {
        var migration = new FieldService.Superset.Migrations.UseCloudContainer();
        var operations = migration.UpOperations.ToList();
        var copy = Assert.IsType<SqlOperation>(operations[0]);
        Assert.Contains("INSERT INTO \"Container\"", copy.Sql);
        Assert.Contains("\"ProviderType\"", copy.Sql);
        Assert.Contains("'superset_' || replace(\"TenantId\"::text, '-', '')", copy.Sql);
        Assert.Contains("\"ExecutionWindow\"", copy.Sql);
        Assert.True(operations.FindIndex(x => x is DropTableOperation) > 0);
        var foreignKey = Assert.IsType<AddForeignKeyOperation>(operations[^1]);
        Assert.Equal("Container", foreignKey.PrincipalTable);

        var rollback = migration.DownOperations.ToList();
        var restoreIndex = rollback.FindIndex(x => x is SqlOperation sql &&
            sql.Sql.Contains("INSERT INTO \"SupersetContainer\""));
        Assert.True(restoreIndex > 0);
        Assert.IsType<AddForeignKeyOperation>(rollback[restoreIndex + 1]);
        var cleanup = Assert.IsType<SqlOperation>(rollback[^1]);
        Assert.Contains("DELETE FROM \"Container\"", cleanup.Sql);
        Assert.Contains("WHERE \"Name\" = 'superset_'", cleanup.Sql);
    }

    [Fact]
    public async Task SupersetRejectsContainerWritesEvenWhenTrackedThroughItsContext()
    {
        using var context = new SupersetDbContext(new DbContextOptionsBuilder<SupersetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var container = CreateContainer(Guid.NewGuid(), "test");
        context.Attach(container);
        container.Status = ContainerStatus.Suspended;

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        context.ChangeTracker.Clear();
        context.Add(container);
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        context.Entry(container).State = EntityState.Deleted;
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    private static async Task<object?> LoadTenant(Type repositoryType, object repository, Guid tenantId)
    {
        var task = (Task)repositoryType.GetMethod("GetSupersetTenantByIdAsync")!
            .Invoke(repository, [tenantId, CancellationToken.None])!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private static Container CreateContainer(Guid tenantId, string name, ExecutionType executionType = ExecutionType.OnDemand) =>
        new(Guid.NewGuid(), tenantId, name, default(CloudProvider), Guid.NewGuid(),
            $"resource-{name}", "https://example.com", executionType, 1, 0, ContainerStatus.Active);

    private sealed class SavingUnitOfWork(SupersetDbContext context) : ISqlUnitOfWork<SupersetDbContext>
    {
        public bool HasActiveTransaction => false;
        public IClientSessionHandle? Session => null;
        public async Task PersistChangesAsync(CancellationToken ct = default) => await context.SaveChangesAsync(ct);
        public Task BeginAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task CommitAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task RollbackAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public void OnCommitted(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void OnRolledBack(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
    }
}
