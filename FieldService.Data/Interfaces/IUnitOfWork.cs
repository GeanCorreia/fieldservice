using MongoDB.Driver;

namespace FieldService.Data.Interfaces;

public interface IUnitOfWork
{
    bool HasActiveTransaction { get; }
    IClientSessionHandle? Session { get; }
    Task PersistChangesAsync(CancellationToken ct = default);
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
