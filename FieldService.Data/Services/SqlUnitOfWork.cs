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
        }
        catch
        {
            try
            {
                await _transaction.RollbackAsync(ct);
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
        await _transaction.DisposeAsync();
        _transaction = null;
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

    private IReadOnlyCollection<EntityEntry> GetTrackedEntries()
    {
        return dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList()
            .AsReadOnly();
    }
}
