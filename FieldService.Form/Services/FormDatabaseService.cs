using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using FieldService.Form.Interfaces;
using FieldService.Form.Entities;
using FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKey;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKeyById;
using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FieldService.Form.Services;

internal sealed record FormDatabaseParams(
    string DatabaseName,
    string Username,
    string Password,
    string TenantDbConnectionString
);

internal sealed class FormDatabaseService : IFormDatabaseService
{
    private const string PostgreSqlAdminDatabase = "postgres";

    private readonly DbConnectionStringBuilder _hostConnectionString;
    private readonly IFormDataDbContextFactory _formDataDbContextFactory;
    private readonly IMediator _mediator;

    public FormDatabaseService(
        IFormDataDbContextFactory formDataDbContextFactory,
        IMediator mediator,
        IConfiguration configuration)
    {
        _formDataDbContextFactory = formDataDbContextFactory ?? throw new ArgumentNullException(nameof(formDataDbContextFactory));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
            FormTenant.FormSecretName(tenantId),
            false);

        var dto = new SecretKeyDto(reference, new ConnectionStringSecret(connectionString));

        await _mediator.Send(new CreateSecretKeyCommand(dto), cancellationToken);
    }

    private (Guid ConnectionStringId, FormDatabaseParams DatabaseParams) CreateDatabaseConnectionString(
        Guid tenantId,
        DbConnectionStringBuilder? dedicatedDbConnectionString = null)
    {
        var secretName = FormTenant.FormSecretName(tenantId);
        var connectionStringId = CreateDeterministicConnectionStringId(tenantId, secretName);
        var userName = $"form_rw_{tenantId:N}";
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var databaseName = FormTenant.DatabaseName(tenantId);
        var tenantConnectionStringBuilder = CreateTenantUserConnectionStringBuilder(
            dedicatedDbConnectionString ?? _hostConnectionString,
            databaseName,
            userName,
            password);

        var databaseParams = new FormDatabaseParams(
            databaseName,
            userName,
            password,
            tenantConnectionStringBuilder.ConnectionString);

        return (connectionStringId, databaseParams);
    }

    private async Task<ConnectionStringSecret?> TryGetExistingConnectionStringSecretAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var secret = await _mediator.Send(
                new GetSecretKeyQuery<ConnectionStringSecret>(tenantId, FormTenant.FormSecretName(tenantId)),
                cancellationToken);

            return string.IsNullOrWhiteSpace(secret.ConnectionString) ? null : secret;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public async Task<Guid> CreateFormDataInfraAsync(
        Guid tenantId,
        Guid? customHostConnectionStringId = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var secretName = FormTenant.FormSecretName(tenantId);
        var connectionStringId = CreateDeterministicConnectionStringId(tenantId, secretName);
        DbConnectionStringBuilder? customHostConnectionString = null;

        if (customHostConnectionStringId.HasValue)
        {
            var hostConnectionString = await _mediator
                .Send(new GetSecretKeyByIdQuery<ConnectionStringSecret>(customHostConnectionStringId.Value), cancellationToken);

            if (hostConnectionString == null || string.IsNullOrWhiteSpace(hostConnectionString.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"No connection string found for tenant {tenantId} with connection string ID {customHostConnectionStringId.Value}.");
            }

            customHostConnectionString = new DbConnectionStringBuilder
            {
                ConnectionString = hostConnectionString.ConnectionString
            };
        }

        var hostConnectionStringBuilder = CloneConnectionStringBuilder(customHostConnectionString ?? _hostConnectionString);
        var connectionStringResult = CreateDatabaseConnectionString(tenantId, hostConnectionStringBuilder);
        var formDatabaseParams = connectionStringResult.DatabaseParams;
        var databaseName = formDatabaseParams.DatabaseName;
        var user = formDatabaseParams.Username;
        var password = formDatabaseParams.Password;
        var tenantConnectionStringBuilder = CreateDatabaseScopedConnectionStringBuilder(hostConnectionStringBuilder, databaseName);
        var sanitizedDbName = EscapeIdentifier(databaseName);
        var sanitizedUserName = EscapeIdentifier(user);
        var sanitizedPassword = EscapeLiteral(password);
        var userLiteral = EscapeLiteral(user);
        var sanitizedSchema = EscapeIdentifier(FormTenant.DatabaseSchema);

        // OutboxPattern Adaptation: Check if secret already exists
        var existingSecret = await TryGetExistingConnectionStringSecretAsync(tenantId, cancellationToken);

        // Step 1: Ensure database exists
        if (!await DatabaseExistsAsync(databaseName, hostConnectionStringBuilder, cancellationToken))
        {
            try
            {
                await ExecuteCommandAsync(
                    $"CREATE DATABASE \"{sanitizedDbName}\";",
                    hostConnectionStringBuilder,
                    cancellationToken: cancellationToken);
            }
            catch (DbException)
            {
                if (!await DatabaseExistsAsync(databaseName, hostConnectionStringBuilder, cancellationToken))
                {
                    throw;
                }
            }
        }

        // Step 2: Persist secret in domain BEFORE creating infrastructure
        // This is the "Outbox" checkpoint - once persisted, we guarantee recovery on retry
        if (existingSecret is null)
        {
            await PersistConnectionStringSecretAsync(
                connectionStringId,
                tenantId,
                formDatabaseParams.TenantDbConnectionString,
                cancellationToken);
        }

        // Step 3: Ensure infrastructure (role, schema, permissions) exists
        // This is idempotent - can be safely retried if step 2 succeeds
        await EnsureInfrastructureSetupAsync(
            tenantConnectionStringBuilder,
            sanitizedDbName,
            sanitizedUserName,
            sanitizedPassword,
            userLiteral,
            sanitizedSchema,
            cancellationToken);

        // Step 4: Ensure migrations are applied
        await EnsureFormDataMigrationsAsync(tenantId, cancellationToken);

        return connectionStringId;
    }

    public async Task EnsureFormDataInfraCheckpointAsync(
        Guid tenantId,
        Guid connectionStringId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (connectionStringId == default)
            throw new ArgumentException("ConnectionStringId is required.", nameof(connectionStringId));

        var expectedConnectionStringId = CreateDeterministicConnectionStringId(
            tenantId,
            FormTenant.FormSecretName(tenantId));

        if (connectionStringId != expectedConnectionStringId)
        {
            throw new InvalidOperationException(
                $"ConnectionStringId '{connectionStringId}' does not match the expected id for tenant '{tenantId}'.");
        }

        // Verify the secret exists (critical for recovery)
        var existingSecret = await TryGetExistingConnectionStringSecretAsync(tenantId, cancellationToken);
        if (existingSecret is null)
        {
            throw new InvalidOperationException(
                $"Secret not found for tenant '{tenantId}'. Infrastructure checkpoint failed - secret must exist.");
        }

        // Reconstruct connection string from secret to validate infrastructure
        var tenantConnectionStringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = existingSecret.ConnectionString
        };

        var databaseName = FormTenant.DatabaseName(tenantId);

        // Validate that critical components exist, attempt to recreate if needed
        if (!await SchemaExistsAsync(FormTenant.DatabaseSchema, tenantConnectionStringBuilder, cancellationToken))
        {
            // If secret exists but schema is missing, attempt to recreate infrastructure
            // Extract credentials from existing secret to rebuild
            var storedUserName = tenantConnectionStringBuilder["Username"] as string;
            var storedPassword = tenantConnectionStringBuilder["Password"] as string;

            if (!string.IsNullOrWhiteSpace(storedUserName) && !string.IsNullOrWhiteSpace(storedPassword))
            {
                // Recreate the infrastructure using extracted credentials
                var sanitizedDbName = EscapeIdentifier(databaseName);
                var sanitizedUserName = EscapeIdentifier(storedUserName);
                var sanitizedPassword = EscapeLiteral(storedPassword);
                var userLiteral = EscapeLiteral(storedUserName);
                var sanitizedSchema = EscapeIdentifier(FormTenant.DatabaseSchema);

                await EnsureInfrastructureSetupAsync(
                    tenantConnectionStringBuilder,
                    sanitizedDbName,
                    sanitizedUserName,
                    sanitizedPassword,
                    userLiteral,
                    sanitizedSchema,
                    cancellationToken);
            }
        }

        // Ensure migrations are applied
        await EnsureFormDataMigrationsAsync(tenantId, cancellationToken);
    }

      private async Task EnsureFormDataMigrationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _formDataDbContextFactory.GetFormDataDbContextAsync(tenantId, cancellationToken);

        try
        {
            // Check if there are pending migrations before attempting to run them
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            
            if (!pendingMigrations.Any())
            {
                // No pending migrations - database is already up to date
                return;
            }

            // Pending migrations exist - apply them idempotently
            // MigrateAsync is safe to retry; it tracks applied migrations in __EFMigrationsHistory
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log migration failure but don't fail the entire operation
            // The infrastructure was created successfully (OutboxPattern checkpoint passed)
            // Migration can be retried on next execution
            throw new InvalidOperationException(
                $"Failed to apply pending migrations for tenant '{tenantId}'. " +
                $"Infrastructure was created successfully. Migration can be retried on next execution.",
                ex);
        }
    }

    private static Guid CreateDeterministicConnectionStringId(
        Guid tenantId,
        string secretName)
    {
        var input = Encoding.UTF8.GetBytes($"{tenantId:N}:{secretName}");
        var hash = SHA256.HashData(input);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | (5 << 4));
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    private async Task EnsureInfrastructureSetupAsync(
        DbConnectionStringBuilder tenantConnectionStringBuilder,
        string sanitizedDbName,
        string sanitizedUserName,
        string sanitizedPassword,
        string userLiteral,
        string sanitizedSchema,
        CancellationToken cancellationToken = default)
    {
        // This method is idempotent and safe to retry
        // All operations use IF NOT EXISTS or ALTER patterns
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
            $"CREATE SCHEMA IF NOT EXISTS \"{sanitizedSchema}\";",
            string.Empty,
            $"GRANT CONNECT ON DATABASE \"{sanitizedDbName}\" TO \"{sanitizedUserName}\";",
            string.Empty,
            $"ALTER SCHEMA \"{sanitizedSchema}\" OWNER TO \"{sanitizedUserName}\";",
            $"GRANT USAGE, CREATE ON SCHEMA \"{sanitizedSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA \"{sanitizedSchema}\" TO \"{sanitizedUserName}\";",
            $"GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA \"{sanitizedSchema}\" TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedSchema}\" GRANT ALL PRIVILEGES ON TABLES TO \"{sanitizedUserName}\";",
            $"ALTER DEFAULT PRIVILEGES IN SCHEMA \"{sanitizedSchema}\" GRANT ALL PRIVILEGES ON SEQUENCES TO \"{sanitizedUserName}\";"
        });

        await ExecuteCommandAsync(
            setupTenantDbSql,
            tenantConnectionStringBuilder,
            cancellationToken: cancellationToken);
    }

    private async Task<bool> DatabaseExistsAsync(
        string databaseName,
        DbConnectionStringBuilder? connectionStringBuilder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var adminConnectionStringBuilder = CreateAdministrativeConnectionStringBuilder(connectionStringBuilder);
        var sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName) THEN 1 ELSE 0 END";

        await using var connection = await CreateAndOpenConnectionAsync(adminConnectionStringBuilder, cancellationToken);
        await using var command = CreateCommand(connection, sql, new { databaseName });
        var exists = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(exists) == 1;
    }

    private async Task<bool> SchemaExistsAsync(
        string schemaName,
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = @schemaName) THEN 1 ELSE 0 END";

        await using var connection = await CreateAndOpenConnectionAsync(connectionStringBuilder, cancellationToken);
        await using var command = CreateCommand(connection, sql, new { schemaName });
        var exists = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(exists) == 1;
    }

    private async Task ExecuteCommandAsync(
        string sqlCommand,
        DbConnectionStringBuilder connectionStringBuilder,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await CreateAndOpenConnectionAsync(connectionStringBuilder, cancellationToken);
        await using var command = CreateCommand(connection, sqlCommand, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }


    private DbConnectionStringBuilder CreateAdministrativeConnectionStringBuilder(DbConnectionStringBuilder? connectionStringBuilder = null)
    {
        var builder = CloneConnectionStringBuilder(connectionStringBuilder);
        builder["Database"] = PostgreSqlAdminDatabase;
        return builder;
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

    private static DbConnectionStringBuilder CloneConnectionStringBuilder(DbConnectionStringBuilder? source = null)
    {
        return new DbConnectionStringBuilder
        {
            ConnectionString = source?.ConnectionString ?? string.Empty
        };
    }

    private static async Task<NpgsqlConnection> CreateAndOpenConnectionAsync(
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static NpgsqlCommand CreateCommand(
        NpgsqlConnection connection,
        string sql,
        object? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        if (parameters is not null)
        {
            foreach (var property in parameters.GetType().GetProperties())
            {
                command.Parameters.AddWithValue(property.Name, property.GetValue(parameters) ?? DBNull.Value);
            }
        }

        return command;
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



