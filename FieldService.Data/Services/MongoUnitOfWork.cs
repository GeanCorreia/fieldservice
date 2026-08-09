using FieldService.Data.Interfaces;
using MongoDB.Driver;

namespace FieldService.Data.Services;

internal sealed class MongoUnitOfWork(
    IMongoClient mongoClient,
    IEntityChangeCollector? changeCollector = null) : IMongoUnitOfWork, IDisposable
{
    private readonly IEntityChangeCollector? _changeCollector = changeCollector;

    public IClientSessionHandle? Session { get; private set; }
    public bool HasActiveTransaction => Session is { IsInTransaction: true };

    public Task PersistChangesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (HasActiveTransaction)
            throw new InvalidOperationException("A transaction is already active for this scope.");

        Session?.Dispose();
        Session = await mongoClient.StartSessionAsync(cancellationToken: ct);
        Session.StartTransaction();
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (!HasActiveTransaction || Session is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await Session.CommitTransactionAsync(ct);
        Session.Dispose();
        Session = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (!HasActiveTransaction || Session is null)
            return;

        await Session.AbortTransactionAsync(ct);
        Session.Dispose();
        Session = null;
    }

    public void Dispose()
    {
        Session?.Dispose();
        Session = null;
    }
}
