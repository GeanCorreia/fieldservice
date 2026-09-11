using FieldService.Data.Interfaces;
using MongoDB.Driver;

namespace FieldService.Data.Services;

internal sealed class MongoUnitOfWork(
    IMongoClient mongoClient,
    IEntityChangeCollector? changeCollector = null) : IMongoUnitOfWork, IDisposable
{
    private readonly IEntityChangeCollector? _changeCollector = changeCollector;
    private readonly List<Func<CancellationToken, Task>> _committedCallbacks = [];
    private readonly List<Func<CancellationToken, Task>> _rolledBackCallbacks = [];

    public IClientSessionHandle? Session { get; private set; }
    public bool HasActiveTransaction => Session is { IsInTransaction: true };

    public Task PersistChangesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return ExecuteCallbacksAsync(_committedCallbacks, ct);
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
        await ExecuteCallbacksAsync(_committedCallbacks, ct);
        Session.Dispose();
        Session = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (!HasActiveTransaction || Session is null)
            return;

        await Session.AbortTransactionAsync(ct);
        await ExecuteCallbacksAsync(_rolledBackCallbacks, ct);
        Session.Dispose();
        Session = null;
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
        Session?.Dispose();
        Session = null;
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
}
