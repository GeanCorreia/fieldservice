using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.Resources;
using Dapper;
using FieldService.Superset.Interfaces;
using FieldService.Shared.Configuration;
using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using Refit;

namespace FieldService.Superset.Services;

internal sealed record SupersetDatabaseParams(
    string DatabaseName,
    string Username,
    string Password
);

internal sealed record SupersetContainerCreationResult(
    string ResourceId,
    string FqdnUrl
);

internal class SupersetTenantDeploymentService : ISupersetTenantDeploymentService
{
    private readonly string _connectionString;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetService _supersetService;
    private readonly ArmClient _armClient;
    private readonly SupersetOptions _supersetOptions;
    private readonly AzureIdentityOptions _azureIdentityOptions;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly string _masterUsername;
    private readonly string _masterPassword;
    private readonly string _host;
    private readonly int _port;

    public SupersetTenantDeploymentService(
        ISupersetApi supersetApi,
        ISupersetAuthService supersetAuthService,
        ISupersetService supersetService,
        IOptions<SupersetOptions> supersetOptions,
        IOptions<AzureIdentityOptions> azureIdentityOptions,
        IConfiguration configuration,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock)
    {
        _connectionString = configuration.GetConnectionString("SupersetHostDatabase")
                            ?? throw new NullReferenceException("Connection string 'SupersetHostDatabase' not found in configuration.");
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _armClient = new ArmClient(new DefaultAzureCredential());
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _masterUsername = _supersetOptions.Username;
        _masterPassword = _supersetOptions.Password;
        _host = _supersetOptions.DataBaseHost.Host;
        _port = _supersetOptions.DataBaseHost.Port;
        _azureIdentityOptions = (azureIdentityOptions ?? throw new ArgumentNullException(nameof(azureIdentityOptions))).Value;
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task<SupersetTenantConfigParams> CreateInstanceAsync(
        SupersetTenantCreateParams supersetTenantCreateParams,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(supersetTenantCreateParams);

        var tenantId = supersetTenantCreateParams.TenantId;
        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Could not acquire lock for tenant {tenantId}. " +
                                                $"Another instance creation process might be running.");
        }

        try
        {
            var existingContainer = await GetExistingContainerAsync(tenantId, cancellationToken);
            if (existingContainer != null)
            {
                var existingConfig = await _supersetService.GetSupersetTenantByTenantIdAsync(tenantId, cancellationToken);
                var existingFqdn = existingContainer.Data.Configuration?.Ingress?.Fqdn;

                throw new InvalidOperationException(
                    $"Superset Container App '{existingContainer.Data.Name}' already exists for tenant {tenantId} " +
                    $"(resourceId: {existingContainer.Id}). Provisioning was aborted to avoid overwriting the existing Superset configuration" +
                    (string.IsNullOrWhiteSpace(existingFqdn) ? "." : $". FQDN: {NormalizeBaseUrl(existingFqdn)}") +
                    (existingConfig == null ? " No tenant configuration was found in the application database for this existing container." : string.Empty));
            }

            var userId = supersetTenantCreateParams.UserId;
            var connectionString = await _supersetAuthService.
                CreateDatabaseConnectionString(userId, tenantId, cancellationToken);
            
            await CreateDatabaseAsync(
                connectionString.databaseParams, 
                connectionString.ConnectionString,
                cancellationToken);
            
            var secretKey = await _supersetAuthService.
                CreateSupersetSecretApiKey(userId, tenantId, cancellationToken);

            var container = await CreateSupersetContainer(
                tenantId,
                secretKey.Key,
                connectionString.ConnectionString,
                cancellationToken);

            await WaitForSupersetReadinessAsync(container.FqdnUrl, cancellationToken);

            if (_supersetOptions.Provisioning.ScaleDownAfterProvisioning)
            {
                await ScaleContainerAsync(container.ResourceId, minReplicas: 0, maxReplicas: 1, cancellationToken);
                await EnsureScaleAsync(container.ResourceId, expectedMinReplicas: 0, expectedMaxReplicas: 1, cancellationToken);
            }

            return new SupersetTenantConfigParams(
                tenantId,
                supersetTenantCreateParams.InstanceTier,
                secretKey.KeyId,
                connectionString.ConnectionStringId,
                container.ResourceId, 
                container.FqdnUrl);
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
        }
    }

    private async Task<SupersetContainerCreationResult> CreateSupersetContainer(
        Guid tenantId,
        string secretKey,
        string connectionString,
        CancellationToken cancellationToken)
    {
        var resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(
            _azureIdentityOptions.AzureSubscriptionId,
            _azureIdentityOptions.AzureResourceGroupName
        );
        
        var resourceGroup = _armClient.GetResourceGroupResource(resourceGroupResourceId);
        var containerAppCollection = resourceGroup.GetContainerApps();
        var containerAppName = SupersetTenantConfig.Container(tenantId);
        
        var containerAppProps = new ContainerAppData(_azureIdentityOptions.AzureLocation)
        {
            EnvironmentId = new ResourceIdentifier(_azureIdentityOptions.AzureContainerAppEnvironmentId),
            Configuration = new ContainerAppConfiguration
            {
                Ingress = new ContainerAppIngressConfiguration
                {
                    External = true,
                    TargetPort = _supersetOptions.Provisioning.ContainerPort,
                    Transport = ContainerAppIngressTransportMethod.Auto
                }
            },
            Template = new ContainerAppTemplate
            {
                Scale = new ContainerAppScale
                {
                    MinReplicas = 1,
                    MaxReplicas = 1
                },
                Containers =
                {
                    new ContainerAppContainer
                    {
                        Name = "superset",
                        Image = _supersetOptions.ContainerAppResourceRequirements.SupersetDockerImage,
                        Command =
                        {
                            "/bin/sh",
                            "-c",
                            BuildBootstrapCommand()
                        },
                        Env =
                        {
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_ENV", Value = "production" },
                            new ContainerAppEnvironmentVariable { Name = "TENANT_ID", Value = tenantId.ToString() },
                            new ContainerAppEnvironmentVariable { Name = "DATABASE_URL", Value = connectionString },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_METADATA_DATABASE_URL", Value = connectionString },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_SECRET_KEY", Value = secretKey },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_ADMIN_USERNAME", Value = _masterUsername },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_ADMIN_PASSWORD", Value = _masterPassword },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_ADMIN_EMAIL", Value = _supersetOptions.Provisioning.AdminEmail },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_ADMIN_FIRST_NAME", Value = _supersetOptions.Provisioning.AdminFirstName },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_ADMIN_LAST_NAME", Value = _supersetOptions.Provisioning.AdminLastName },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_METADATA_SCHEMA", Value = SupersetTenantConfig.MetadataSchemaPrefix },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_DATA_SCHEMA", Value = SupersetTenantConfig.DataSchemaPrefix },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_MOCKED_DATA_SCHEMA", Value = SupersetTenantConfig.MockedDataSchemaPrefix },
                            new ContainerAppEnvironmentVariable { Name = "SUPERSET_SCHEMA_ACCESS_MODE", Value = "exclusive" }
                        }
                    }
                }
            }
        };

        var armOperation = await containerAppCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            containerAppName,
            containerAppProps,
            cancellationToken);

        var createdContainerApp = armOperation.Value;
        var resourceId = createdContainerApp.Id.ToString();
        var fqdnUrl = await ResolveFqdnUrlAsync(createdContainerApp, cancellationToken);

        return new SupersetContainerCreationResult(resourceId, fqdnUrl);
    }

    private async Task<ContainerAppResource?> GetExistingContainerAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(
            _azureIdentityOptions.AzureSubscriptionId,
            _azureIdentityOptions.AzureResourceGroupName);

        var resourceGroup = _armClient.GetResourceGroupResource(resourceGroupResourceId);
        var containerAppCollection = resourceGroup.GetContainerApps();
        var containerAppName = SupersetTenantConfig.Container(tenantId);
        var existsResponse = await containerAppCollection.ExistsAsync(containerAppName, cancellationToken);

        if (!existsResponse.Value)
        {
            return null;
        }

        var containerResponse = await containerAppCollection.GetAsync(containerAppName, cancellationToken);
        return containerResponse.Value;
    }
   
    private async Task CreateDatabaseAsync(
        SupersetDatabaseParams supersetDatabaseParams,
        string tenantDbConnectionString,
        CancellationToken cancellationToken = default)
    {
        var databaseName = supersetDatabaseParams.DatabaseName;
        var user = supersetDatabaseParams.Username;
        var password = supersetDatabaseParams.Password;
        
        await using (var adminConnection = new NpgsqlConnection(_connectionString))
        {
            await adminConnection.OpenAsync(cancellationToken);

            try
            {
                var sanitizedDbName = databaseName.Replace("\"", "\"\"");
                var createDbSql = $"CREATE DATABASE \"{sanitizedDbName}\";";
                
                await adminConnection.ExecuteAsync(new CommandDefinition(createDbSql, cancellationToken: cancellationToken));
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateDatabase)
            {
                // Database já existe, segue o fluxo
            }
        }

        
        string Lit(string val) => $"'{val.Replace("'", "''")}'"; 
        string Id(string val) => val.Replace("\"", "\"\"");     
        
        var setupTenantDbSql = $@"
            -- Criação dos Schemas
            CREATE SCHEMA IF NOT EXISTS ""{Id(SupersetTenantConfig.DataSchemaPrefix)}"";
            CREATE SCHEMA IF NOT EXISTS ""{Id(SupersetTenantConfig.MetadataSchemaPrefix)}"";
            CREATE SCHEMA IF NOT EXISTS ""{Id(SupersetTenantConfig.MockedDataSchemaPrefix)}"";

            -- Gestão de Usuário e Senha com segurança via PL/pgSQL
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = {Lit(user)}) THEN
                    EXECUTE format('CREATE USER %I WITH PASSWORD %L', {Lit(user)}, {Lit(password)});
                ELSE
                    EXECUTE format('ALTER USER %I WITH PASSWORD %L', {Lit(user)}, {Lit(password)});
                END IF;
            END
            $$;

            -- Permissão de Conexão no Banco
            EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', CURRENT_DATABASE(), {Lit(user)});

            -----------------------------------------------------------------------------
            -- 1. SCHEMA METADATA (Escrita / Ownership Total para o Superset)
            -----------------------------------------------------------------------------
            EXECUTE format('ALTER SCHEMA %I OWNER TO %I', {Lit(SupersetTenantConfig.MetadataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('GRANT ALL ON SCHEMA %I TO %I', {Lit(SupersetTenantConfig.MetadataSchemaPrefix)}, {Lit(user)});

            -----------------------------------------------------------------------------
            -- 2. SCHEMAS DATA e MOCKED DATA (Apenas Leitura / Read-Only para o Superset)
            -----------------------------------------------------------------------------
            -- Permissão de navegação no Schema
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});

            -- Permissão SELECT em tabelas existentes (se houver)
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});

            -- Permissão SELECT em tabelas FUTURAS criadas pelo seu módulo de Injeção de Dados (Admin)
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON TABLES TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON TABLES TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});

            -- Permissão SELECT em SEQUENCES FUTURAS (Auto-Increment)
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON SEQUENCES TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON SEQUENCES TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});
        ";

        await using var connection = new NpgsqlConnection(tenantDbConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(setupTenantDbSql, cancellationToken: cancellationToken));
    }

    private string BuildBootstrapCommand()
    {
        return $$"""
        set -eu

        cat <<'EOF' >/app/pythonpath/superset_config.py
        import os

        SECRET_KEY = os.environ["SUPERSET_SECRET_KEY"]
        SQLALCHEMY_DATABASE_URI = os.environ["SUPERSET_METADATA_DATABASE_URL"]
        METADATA_SCHEMA = os.environ.get("SUPERSET_METADATA_SCHEMA", "Metadata")

        # Força o SQLAlchemy a utilizar o Schema de Metadados isolado por padrão
        SQLALCHEMY_ENGINE_OPTIONS = {
            "connect_args": {
                "options": f"-c search_path={METADATA_SCHEMA},public"
            }
        }

        FEATURE_FLAGS = {"EMBEDDED_SUPERSET": True}
        EOF

        superset db upgrade
        
        # Garante a criação ou atualização do usuário Admin sem silenciar erros críticos
        superset fab create-admin \
          --username "$SUPERSET_ADMIN_USERNAME" \
          --firstname "$SUPERSET_ADMIN_FIRST_NAME" \
          --lastname "$SUPERSET_ADMIN_LAST_NAME" \
          --email "$SUPERSET_ADMIN_EMAIL" \
          --password "$SUPERSET_ADMIN_PASSWORD" || true

        superset init

        exec gunicorn \
          --bind "0.0.0.0:{{_supersetOptions.Provisioning.ContainerPort}}" \
          --workers "{{_supersetOptions.Provisioning.GunicornWorkers}}" \
          --timeout "{{_supersetOptions.Provisioning.GunicornTimeoutSeconds}}" \
          "superset.app:create_app()"
        """;
    }

    private async Task<string> ResolveFqdnUrlAsync(
        ContainerAppResource containerAppResource,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _supersetOptions.Provisioning.StartupHealthCheckMaxAttempts; attempt++)
        {
            var response = await containerAppResource.GetAsync(cancellationToken);
            var fqdn = response.Value.Data.Configuration?.Ingress?.Fqdn;

            if (!string.IsNullOrWhiteSpace(fqdn))
            {
                return NormalizeBaseUrl(fqdn);
            }

            await Task.Delay(TimeSpan.FromSeconds(_supersetOptions.Provisioning.StartupHealthCheckDelaySeconds), cancellationToken);
        }

        throw new TimeoutException("Timed out while waiting for the Superset Container App ingress FQDN to be assigned.");
    }

    private async Task WaitForSupersetReadinessAsync(
        string fqdnUrl,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _supersetOptions.Provisioning.StartupHealthCheckMaxAttempts; attempt++)
        {
            try
            {
                await _supersetApi.GetHealthAsync(new Uri(fqdnUrl), cancellationToken);
                _ = await _supersetAuthService.GetAdminTokenApi(fqdnUrl, cancellationToken);
                return;
            }
            catch (ApiException ex)
            {
                lastException = ex;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(_supersetOptions.Provisioning.StartupHealthCheckDelaySeconds), cancellationToken);
        }

        throw new TimeoutException(
            $"Superset container at '{fqdnUrl}' did not become healthy after {_supersetOptions.Provisioning.StartupHealthCheckMaxAttempts} attempts.",
            lastException);
    }

    private async Task ScaleContainerAsync(
        string resourceId,
        int minReplicas,
        int maxReplicas,
        CancellationToken cancellationToken)
    {
        var identifier = new ResourceIdentifier(resourceId);
        var containerAppResource = _armClient.GetContainerAppResource(identifier);
        var containerApp = await containerAppResource.GetAsync(cancellationToken);

        containerApp.Value.Data.Template ??= new ContainerAppTemplate();
        containerApp.Value.Data.Template.Scale ??= new ContainerAppScale();
        containerApp.Value.Data.Template.Scale.MinReplicas = minReplicas;
        containerApp.Value.Data.Template.Scale.MaxReplicas = maxReplicas;

        await containerAppResource.UpdateAsync(WaitUntil.Completed, containerApp.Value.Data, cancellationToken);
    }

    private async Task EnsureScaleAsync(
        string resourceId,
        int expectedMinReplicas,
        int expectedMaxReplicas,
        CancellationToken cancellationToken)
    {
        var identifier = new ResourceIdentifier(resourceId);
        var containerAppResource = _armClient.GetContainerAppResource(identifier);
        var containerApp = await containerAppResource.GetAsync(cancellationToken);
        var scale = containerApp.Value.Data.Template?.Scale;

        if (scale == null || scale.MinReplicas != expectedMinReplicas || scale.MaxReplicas != expectedMaxReplicas)
        {
            throw new InvalidOperationException(
                $"Container App scale validation failed for resource '{resourceId}'. Expected min/max replicas {expectedMinReplicas}/{expectedMaxReplicas}.");
        }
    }

    private static string NormalizeBaseUrl(string fqdn)
    {
        if (fqdn.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            fqdn.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return fqdn;
        }

        return $"https://{fqdn}";
    }
}