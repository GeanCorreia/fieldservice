namespace FieldService.Superset.Interfaces;

public interface IDatabaseService
{
    Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken cancellationToken = default);

    Task<bool> SchemaExistsAsync(string databaseName, string schemaName, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> QueryAsync<T>(string databaseName, string sql, object? parameters = null, CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(string databaseName, string sql, object? parameters = null, CancellationToken cancellationToken = default);

    Task ExecuteCommandAsync(string sqlCommand,string? databaseName = null, object? parameters = null, CancellationToken cancellationToken = default);
}