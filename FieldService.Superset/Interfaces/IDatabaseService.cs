using System.Data.Common;

namespace FieldService.Superset.Interfaces;

public enum DatabaseProviderType
{
    PostgreSql = 1,
    SqlServer = 2
}

public interface IDatabaseService
{
    DatabaseProviderType GetDatabaseProvider(DbConnectionStringBuilder? connectionStringBuilder = null);

    Task<bool> DatabaseExistsAsync(
        string databaseName,
        DbConnectionStringBuilder? connectionStringBuilder = null,
        CancellationToken cancellationToken = default);

    Task<bool> SchemaExistsAsync(
        string schemaName,
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task ExecuteCommandAsync(
        string sqlCommand,
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}