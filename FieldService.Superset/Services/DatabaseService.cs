using System.Data.Common;
using Dapper;
using FieldService.Superset.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FieldService.Superset.Services;

public class DatabaseService : IDatabaseService
{
    private const string PostgreSqlAdminDatabase = "postgres";
    private const string SqlServerAdminDatabase = "master";

    private readonly DbConnectionStringBuilder _baseConnectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _baseConnectionString = new DbConnectionStringBuilder
        {
            ConnectionString = configuration.GetConnectionString("HostDatabase")
                ?? throw new NullReferenceException("Connection string 'HostDatabase' não encontrada na configuração.")
        };
    }
    
    public DatabaseProviderType GetDatabaseProvider(DbConnectionStringBuilder? connectionStringBuilder = null)
    {
        var builder = CloneConnectionStringBuilder(connectionStringBuilder);
        var keys = builder.Keys.Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (keys.Contains("Host") || keys.Contains("Port") || keys.Contains("Username") || keys.Contains("Search Path"))
        {
            return DatabaseProviderType.PostgreSql;
        }

        if (keys.Contains("Data Source") || keys.Contains("Server") || keys.Contains("Initial Catalog") || keys.Contains("TrustServerCertificate"))
        {
            return DatabaseProviderType.SqlServer;
        }

        throw new NotSupportedException("Não foi possível identificar o tipo de banco a partir da connection string informada.");
    }

    public async Task ExecuteCommandAsync(
        string sqlCommand, 
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await CreateAndOpenConnectionAsync(connectionStringBuilder, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sqlCommand, parameters, cancellationToken: cancellationToken));
    }

    public async Task<bool> DatabaseExistsAsync(
        string databaseName,
        DbConnectionStringBuilder? connectionStringBuilder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var adminConnectionStringBuilder = CreateAdministrativeConnectionStringBuilder(connectionStringBuilder);
        var sql = GetDatabaseProvider(adminConnectionStringBuilder) switch
        {
            DatabaseProviderType.PostgreSql => "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName) THEN 1 ELSE 0 END",
            DatabaseProviderType.SqlServer => "SELECT CASE WHEN DB_ID(@databaseName) IS NOT NULL THEN 1 ELSE 0 END",
            _ => throw new NotSupportedException()
        };

        using var connection = await CreateAndOpenConnectionAsync(adminConnectionStringBuilder, cancellationToken);
        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { databaseName }, cancellationToken: cancellationToken)
        );

        return exists == 1;
    }
    
    public async Task<bool> SchemaExistsAsync(
        string schemaName,
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var builder = CloneConnectionStringBuilder(connectionStringBuilder);
        var sql = GetDatabaseProvider(builder) switch
        {
            DatabaseProviderType.PostgreSql => "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = @schemaName) THEN 1 ELSE 0 END",
            DatabaseProviderType.SqlServer => "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.schemas WHERE name = @schemaName) THEN 1 ELSE 0 END",
            _ => throw new NotSupportedException()
        };

        using var connection = await CreateAndOpenConnectionAsync(builder, cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { schemaName }, cancellationToken: cancellationToken)
        );

        return exists == 1;
    }
    
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await CreateAndOpenConnectionAsync(connectionStringBuilder, cancellationToken);
        return await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await CreateAndOpenConnectionAsync(connectionStringBuilder, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private DbConnectionStringBuilder CreateAdministrativeConnectionStringBuilder(DbConnectionStringBuilder? connectionStringBuilder = null)
    {
        var builder = CloneConnectionStringBuilder(connectionStringBuilder);
        SetDatabaseName(builder, GetDatabaseProvider(builder), databaseName: null);
        return builder;
    }

    private DbConnectionStringBuilder CloneConnectionStringBuilder(DbConnectionStringBuilder? connectionStringBuilder = null)
    {
        return new DbConnectionStringBuilder
        {
            ConnectionString = (connectionStringBuilder ?? _baseConnectionString).ConnectionString
        };
    }

    private async Task<DbConnection> CreateAndOpenConnectionAsync(
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection(connectionStringBuilder);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private DbConnection CreateConnection(DbConnectionStringBuilder connectionStringBuilder)
    {
        return GetDatabaseProvider(connectionStringBuilder) switch
        {
            DatabaseProviderType.PostgreSql => new NpgsqlConnection(connectionStringBuilder.ConnectionString),
            DatabaseProviderType.SqlServer => new SqlConnection(connectionStringBuilder.ConnectionString),
            _ => throw new NotSupportedException("Provider de banco não suportado.")
        };
    }

    private static void SetDatabaseName(
        DbConnectionStringBuilder connectionStringBuilder,
        DatabaseProviderType provider,
        string? databaseName)
    {
        switch (provider)
        {
            case DatabaseProviderType.PostgreSql:
                connectionStringBuilder["Database"] = string.IsNullOrWhiteSpace(databaseName)
                    ? PostgreSqlAdminDatabase
                    : databaseName;
                break;

            case DatabaseProviderType.SqlServer:
                connectionStringBuilder["Initial Catalog"] = string.IsNullOrWhiteSpace(databaseName)
                    ? SqlServerAdminDatabase
                    : databaseName;
                break;

            default:
                throw new NotSupportedException("Provider de banco não suportado.");
        }
    }

    
}