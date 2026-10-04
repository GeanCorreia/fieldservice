using System.Data.Common;
using System.Security.Cryptography;
using FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;
using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Entities;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FieldService.Superset.Services;

internal sealed record SupersetDatabaseParams(
    string DatabaseName,
    string Username,
    string Password,
    string tenantDbConnectionString
);

internal class SupersetDataBaseService : ISupersetDataBaseService
{
    private readonly DbConnectionStringBuilder _hostConnectionString;
    private readonly ISupersetTenantInstanceProcessingLock _processingLock;
    private readonly IDatabaseService _databaseService;
    private readonly IMediator _mediator;
    
    public SupersetDataBaseService(
        IDatabaseService databaseService,
        ISupersetTenantInstanceProcessingLock processingLock,
        IMediator mediator,
        IConfiguration configuration)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _processingLock = processingLock ?? throw new ArgumentNullException(nameof(processingLock));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _hostConnectionString = new DbConnectionStringBuilder
        {
            ConnectionString = configuration.GetConnectionString("HostDatabase")
                ?? throw new NullReferenceException("Connection string 'HostDatabase' não encontrada na configuração.")
        };
    }
    
    private async Task PersistConnectionStringSecretAsync(
        Guid connectionStringId,
        Guid tenantId, 
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var reference = new SecretKeyReferenceDto(
            connectionStringId,
            SecretKeyType.ConnectionString,
            tenantId,
            SupersetTenant.ConnectionStringName(tenantId),
            false
        );

        var dto = new SecretKeyDto(reference, new ConnectionStringSecret(connectionString));

        await _mediator.Send(new CreateSecretKeyCommand(dto, null), cancellationToken);
    }

    private (Guid ConnectionStringId, SupersetDatabaseParams DatabaseParams) CreateDatabaseConnectionString(
        Guid tenantId,
        DbConnectionStringBuilder? dedicatedDbConnectionString = null)
    {
        var connectionStringId = Guid.NewGuid();

        var userName = $"superset_ro_{tenantId:N}";
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var databaseName = SupersetTenant.Database(tenantId);
        var tenantConnectionStringBuilder = CreateTenantUserConnectionStringBuilder(
            dedicatedDbConnectionString ?? _hostConnectionString,
            databaseName,
            userName,
            password);

        var databaseParams = new SupersetDatabaseParams(
            databaseName,
            userName,
            password,
            tenantConnectionStringBuilder.ConnectionString);

        return (connectionStringId, databaseParams);
    }

    
    public async Task<Guid> CreateSupersetDataInfra(
        Guid tenantId,
        DbConnectionStringBuilder? dedicatedDbConnectionString = null,
        CancellationToken cancellationToken = default)
    {
        if(! await _processingLock.AcquireLock(tenantId, cancellationToken))
        {
            throw new InvalidOperationException($"It's not possible to acquire the processing lock for tenant {tenantId}.");
        }

        var hostConnectionStringBuilder = CloneConnectionStringBuilder(dedicatedDbConnectionString ?? _hostConnectionString);
        var connectionStringResult = CreateDatabaseConnectionString(tenantId, hostConnectionStringBuilder);
        var connectionStringId = connectionStringResult.ConnectionStringId;
        var supersetDatabaseParams = connectionStringResult.DatabaseParams;
        var databaseName = supersetDatabaseParams.DatabaseName;
        var user = supersetDatabaseParams.Username;
        var password = supersetDatabaseParams.Password;
        var tenantConnectionStringBuilder = CreateDatabaseScopedConnectionStringBuilder(hostConnectionStringBuilder, databaseName);
        var sanitizedDbName = EscapeIdentifier(databaseName);
        var sanitizedUserName = EscapeIdentifier(user);
        var sanitizedPassword = EscapeLiteral(password);
        var userLiteral = EscapeLiteral(user);
        var sanitizedDataSchema = EscapeIdentifier(SupersetTenant.DataSchemaPrefix);
        var sanitizedMetadataSchema = EscapeIdentifier(SupersetTenant.MetadataSchemaPrefix);
        var sanitizedMockedDataSchema = EscapeIdentifier(SupersetTenant.MockedDataSchemaPrefix);
        
        if (!await _databaseService.DatabaseExistsAsync(databaseName, hostConnectionStringBuilder, cancellationToken))
        {
            try
            {
                await _databaseService.ExecuteCommandAsync(
                    $"CREATE DATABASE \"{sanitizedDbName}\";",
                    hostConnectionStringBuilder,
                    cancellationToken: cancellationToken);
            }
            catch (DbException)
            {
                if (!await _databaseService.DatabaseExistsAsync(databaseName, hostConnectionStringBuilder, cancellationToken))
                {
                    throw;
                }
            }
        }

        var setupTenantDbSql = string.Join(Environment.NewLine, new[]
        {
            "DO $$",
            "BEGIN",
            $"    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = '{userLiteral}') THEN",
            $"        CREATE ROLE \"{sanitizedUserName}\" LOGIN PASSWORD '{sanitizedPassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;",
            "    ELSE",
            $"        ALTER ROLE \"{sanitizedUserName}\" WITH LOGIN PASSWORD '{sanitizedPassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;",
            "    END IF;",
            "END",
            "$$;",
            string.Empty,
            $"CREATE SCHEMA IF NOT EXISTS \"{sanitizedDataSchema}\";",
            $"CREATE SCHEMA IF NOT EXISTS \"{sanitizedMetadataSchema}\";",
            $"CREATE SCHEMA IF NOT EXISTS \"{sanitizedMockedDataSchema}\";",
            string.Empty,
            $"GRANT CONNECT ON DATABASE \"{sanitizedDbName}\" TO \"{sanitizedUserName}\";",
            string.Empty,
            $"ALTER SCHEMA \"{sanitizedMetadataSchema}\" OWNER TO \"{sanitizedUserName}\";",
            $"GRANT USAGE, CREATE ON SCHEMA \"{sanitizedMetadataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA \"{sanitizedMetadataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA \"{sanitizedMetadataSchema}\" TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedMetadataSchema}\" GRANT ALL PRIVILEGES ON TABLES TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedMetadataSchema}\" GRANT ALL PRIVILEGES ON SEQUENCES TO \"{sanitizedUserName}\";",
            string.Empty,
            $"REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA \"{sanitizedDataSchema}\" FROM \"{sanitizedUserName}\";",
            $"REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA \"{sanitizedMockedDataSchema}\" FROM \"{sanitizedUserName}\";",
            $"REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA \"{sanitizedDataSchema}\" FROM \"{sanitizedUserName}\";",
            $"REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA \"{sanitizedMockedDataSchema}\" FROM \"{sanitizedUserName}\";",
            string.Empty,
            $"GRANT USAGE ON SCHEMA \"{sanitizedDataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT USAGE ON SCHEMA \"{sanitizedMockedDataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT SELECT ON ALL TABLES IN SCHEMA \"{sanitizedDataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT SELECT ON ALL TABLES IN SCHEMA \"{sanitizedMockedDataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA \"{sanitizedDataSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA \"{sanitizedMockedDataSchema}\" TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedDataSchema}\" GRANT SELECT ON TABLES TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedMockedDataSchema}\" GRANT SELECT ON TABLES TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedDataSchema}\" GRANT USAGE, SELECT ON SEQUENCES TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedMockedDataSchema}\" GRANT USAGE, SELECT ON SEQUENCES TO \"{sanitizedUserName}\";"
        });
        
        
        await _databaseService.ExecuteCommandAsync(
            setupTenantDbSql, 
            tenantConnectionStringBuilder,
            cancellationToken: cancellationToken);

        await PersistConnectionStringSecretAsync(
            connectionStringId,
            tenantId,
            supersetDatabaseParams.tenantDbConnectionString,
            cancellationToken);

        
        return connectionStringId;
    }

    private DbConnectionStringBuilder CreateDatabaseScopedConnectionStringBuilder(
        DbConnectionStringBuilder baseConnectionStringBuilder,
        string databaseName)
    {
        var connectionStringBuilder = CloneConnectionStringBuilder(baseConnectionStringBuilder);
        connectionStringBuilder["Database"] = databaseName;
        return connectionStringBuilder;
    }

    private DbConnectionStringBuilder CreateTenantUserConnectionStringBuilder(
        DbConnectionStringBuilder baseConnectionStringBuilder,
        string databaseName,
        string userName,
        string password)
    {
        var connectionStringBuilder = CreateDatabaseScopedConnectionStringBuilder(baseConnectionStringBuilder, databaseName);
        connectionStringBuilder["Username"] = userName;
        connectionStringBuilder["Password"] = password;
        return connectionStringBuilder;
    }

    private static DbConnectionStringBuilder CloneConnectionStringBuilder(DbConnectionStringBuilder source)
    {
        return new DbConnectionStringBuilder
        {
            ConnectionString = source.ConnectionString
        };
    }

    private static string EscapeIdentifier(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    private static string EscapeLiteral(string value)
    {
        return value.Replace("'", "''");
    }
}