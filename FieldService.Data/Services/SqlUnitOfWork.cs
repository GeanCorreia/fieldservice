using FieldService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;

namespace FieldService.Data.Services;

internal sealed class SqlUnitOfWork<TDbContext>(TDbContext dbContext)
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

        _ = await dbContext.SaveChangesAsync(ct);
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

        _ = await dbContext.SaveChangesAsync(ct);
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
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
}
