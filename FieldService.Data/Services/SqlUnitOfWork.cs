using FieldService.Data.Interfaces;
using FieldService.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace FieldService.Data.Services;

internal sealed class SqlUnitOfWork<TDbContext>(
    TDbContext dbContext, 
    IEntityChangeCollector? changeCollector = null,
    IEntityChangeExtractor? changeExtractor = null)
    : ISqlUnitOfWork<TDbContext>, IDisposable
    where TDbContext : DbContext
{
    private IDbContextTransaction? _transaction;
    private readonly List<Func<CancellationToken, Task>> _committedCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _rolledBackCallbacks = [];

    public bool HasActiveTransaction => _transaction is not null;
    public IClientSessionHandle? Session => null;

    public async Task PersistChangesAsync(CancellationToken ct = default)
    {
        if (HasActiveTransaction)
            return;

        var changes = ExtractTrackedChanges();

        try
        {
            _ = await dbContext.SaveChangesAsync(ct);
            CollectCommittedChanges(changes);
            await ExecuteCallbacksAsync(_committedCallbacks, ct);
        }
        catch
        {
            throw;
        }
    }

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (HasActiveTransaction)
            throw new InvalidOperationException("A transaction is already active for this scope.");

        _transaction = await dbContext.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        var changes = ExtractTrackedChanges();

        try
        {
            _ = await dbContext.SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
            CollectCommittedChanges(changes);
            await ExecuteCallbacksAsync(_committedCallbacks, ct);
        }
        catch
        {
            try
            {
                await _transaction.RollbackAsync(ct);
                await ExecuteCallbacksAsync(_rolledBackCallbacks, ct);
            }
            catch
            {
                // Preserve the original failure from SaveChanges/Commit.
            }

            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(ct);
        await ExecuteCallbacksAsync(_rolledBackCallbacks, ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void OnCommitted(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _committedCallbacks.Add(callback);
    }

    public void OnRolledBack(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _rolledBackCallbacks.Add(callback);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
    }

    private IReadOnlyCollection<CollectedEntityChange> ExtractTrackedChanges()
    {
        if (changeCollector is null || changeExtractor is null)
            return [];

        return changeExtractor.Extract(GetTrackedEntries());
    }

    private void CollectCommittedChanges(IReadOnlyCollection<CollectedEntityChange> changes)
    {
        if (changes.Count == 0)
            return;

        changeCollector?.Collect(changes);
    }

    private static async Task ExecuteCallbacksAsync(
        List<Func<CancellationToken, Task>> callbacks,
        CancellationToken ct)
    {
        if (callbacks.Count == 0)
            return;

        var pendingCallbacks = callbacks.ToArray();
        callbacks.Clear();

        foreach (var callback in pendingCallbacks)
        {
            await callback(ct);
        }
    }

    private IReadOnlyCollection<EntityEntry> GetTrackedEntries()
    {
        return dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList()
            .AsReadOnly();
    }
}
