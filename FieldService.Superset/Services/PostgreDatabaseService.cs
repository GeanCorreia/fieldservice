using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using FieldService.Superset.Interfaces;
using Microsoft.Data.SqlClient;

namespace FieldService.Superset.Services;

public class PostgreDatabaseService : IDatabaseService
{
    private readonly string _baseConnectionString;

    public PostgreDatabaseService(IConfiguration configuration)
    {
        _baseConnectionString = configuration.GetConnectionString("HostDatabase")
            ?? throw new NullReferenceException("Connection string 'HostDatabase' não encontrada na configuração.");
    }
    
    private NpgsqlConnection CreateConnection(string? databaseName = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString);
        
        builder.Database = string.IsNullOrWhiteSpace(databaseName) ? "postgres" : databaseName;

        return new NpgsqlConnection(builder.ConnectionString);
    }

    public async Task ExecuteCommandAsync(
        string sqlCommand, 
        string? databaseName = null, 
        object? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await CreateAndOpenConnectionAsync(databaseName, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sqlCommand, parameters, cancellationToken: cancellationToken));
    }

    private async Task<NpgsqlConnection> CreateAndOpenConnectionAsync(string? databaseName = null, CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection(databaseName);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
    
    public async Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName) THEN 1 ELSE 0 END";
        
        using var connection = await CreateAndOpenConnectionAsync(databaseName: null, cancellationToken);
        
        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { databaseName }, cancellationToken: cancellationToken)
        );

        return exists == 1;
    }
    
    public async Task<bool> SchemaExistsAsync(string databaseName, string schemaName, CancellationToken cancellationToken = default)
    {
        if (!await DatabaseExistsAsync(databaseName, cancellationToken))
        {
            return false;
        }

        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = @schemaName) THEN 1 ELSE 0 END";
        
        using var connection = await CreateAndOpenConnectionAsync(databaseName, cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { schemaName }, cancellationToken: cancellationToken)
        );

        return exists == 1;
    }
    
    public async Task<IEnumerable<T>> QueryAsync<T>(string databaseName, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = await CreateAndOpenConnectionAsync(databaseName, cancellationToken);
        return await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string databaseName, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = await CreateAndOpenConnectionAsync(databaseName, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    
}