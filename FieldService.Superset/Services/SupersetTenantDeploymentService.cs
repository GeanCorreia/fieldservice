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
using FieldService.Superset.Attributes;
using FieldService.Superset.Broker;
using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Jobs;
using FieldService.Superset.Proxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Refit;

namespace FieldService.Superset.Services;

internal class SupersetTenantDeploymentService : ISupersetTenantDeploymentService
{
    private readonly SupersetContainerAllowedOriginsCors _supersetContainerAllowedOriginsCors;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetSecretService _supersetSecretService;
    private readonly ISupersetService _supersetService;
    private readonly ArmClient _armClient;
    private readonly SupersetOptions _supersetOptions;
    private readonly AzureIdentityOptions _azureIdentityOptions;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly CreateSupersetTenantConfigJobProducer _createSupersetTenantConfigJobProducer;
    private readonly ILogger<SupersetTenantDeploymentService> _logger;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly string _masterUsername;
    private readonly string _masterPassword;

    public SupersetTenantDeploymentService(
        ISupersetApi supersetApi,
        ISupersetSecretService supersetSecretService,
        ISupersetService supersetService,
        IOptions<SupersetOptions> supersetOptions,
        IOptions<AzureIdentityOptions> azureIdentityOptions,
        IConfiguration configuration,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        CreateSupersetTenantConfigJobProducer createSupersetTenantConfigJobProducer,
        ILogger<SupersetTenantDeploymentService> logger,
        ISupersetAuthService supersetAuthService,
        SupersetContainerAllowedOriginsCors supersetContainerAllowedOriginsCors)
    {
   
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetSecretService = supersetSecretService ?? throw new ArgumentNullException(nameof(supersetSecretService));
        _supersetContainerAllowedOriginsCors = supersetContainerAllowedOriginsCors ?? 
                                               throw new ArgumentNullException(nameof(supersetContainerAllowedOriginsCors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _createSupersetTenantConfigJobProducer = createSupersetTenantConfigJobProducer ?? throw new ArgumentNullException(nameof(createSupersetTenantConfigJobProducer));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _armClient = new ArmClient(new DefaultAzureCredential());
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _masterUsername = _supersetOptions.Username;
        _masterPassword = _supersetOptions.Password;
        _azureIdentityOptions = (azureIdentityOptions ?? throw new ArgumentNullException(nameof(azureIdentityOptions))).Value;
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task CreateInstanceAsync(
        SupersetTenantCreateParams supersetTenantCreateParams,
        Guid connectionStringId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
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
            
            var connectionString = await _supersetSecretService.GetDatabaseConnectionString(connectionStringId, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string with ID '{connectionStringId}' not found or is empty.");
            }
            
            var secretKey = await _supersetSecretService.
                CreateSupersetSecretApiKey(tenantId, userId, cancellationToken);

            var container = await CreateSupersetContainer(
                tenantId,
                secretKey.Key,
                connectionString,
                SupersetTenantConfig.Database(tenantId),
                supersetTenantCreateParams.InstanceTier,
                supersetTenantCreateParams.ScheduledExecutionWindow,
                cancellationToken);

            await WaitForSupersetReadinessAsync(container.FqdnUrl, tenantId, cancellationToken);
            
            var tenantConfig =  SupersetTenantConfig.Create(
                tenantId,
                supersetTenantCreateParams.InstanceTier,
                connectionStringId,
                container.ResourceId,
                container.FqdnUrl,
                secretKey.KeyId);
            
            var payload = new CreateSupersetTenantConfigPayload(tenantConfig);
            var job = new CreateSupersetTenantConfigJob(payload);
            _createSupersetTenantConfigJobProducer.Publish(job);
            
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
    string tenantDatabaseName,
    InstanceTier instanceTier,
    ScheduledExecutionWindow? executionWindow,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(
            _azureIdentityOptions.AzureSubscriptionId,
            _azureIdentityOptions.AzureResourceGroupName
        );
        
        var resourceGroup = _armClient.GetResourceGroupResource(resourceGroupResourceId);
        var containerAppCollection = resourceGroup.GetContainerApps();
        var containerAppName = SupersetTenantConfig.Container(tenantId);
        
        var supersetContainer = new ContainerAppContainer
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
                new ContainerAppEnvironmentVariable { Name = "AUTH_USER_REGISTRATION", Value = "False" },
                new ContainerAppEnvironmentVariable { Name = "AUTH_TYPE", Value = "AUTH_REMOTE_USER"},
                new ContainerAppEnvironmentVariable { Name = "HTTP_HEADER_REMOTE_USER", Value = SupersetProxyHeaders.RemoteUserHeaderName },
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
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_TENANT_DB_NAME", Value = tenantDatabaseName },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_METADATA_SCHEMA", Value = SupersetTenantConfig.MetadataSchemaPrefix },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_DATA_SCHEMA", Value = SupersetTenantConfig.DataSchemaPrefix },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_MOCKED_DATA_SCHEMA", Value = SupersetTenantConfig.MockedDataSchemaPrefix },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_SCHEMA_ACCESS_MODE", Value = "exclusive" },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_DATA_SCHEMA_ROLE_NAME", Value = "Scope_Data" },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_MOCKED_SCHEMA_ROLE_NAME", Value = "Scope_MockedData" }
            }
        };
        
        foreach (var envVar in _supersetContainerAllowedOriginsCors.Get())
        {
            supersetContainer.Env.Add(envVar);
        }
        
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
                Scale = BuildScaleConfiguration(instanceTier, executionWindow),
                Containers = { supersetContainer }
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
    private ContainerAppScale BuildScaleConfiguration(
        InstanceTier instanceTier,
        ScheduledExecutionWindow? executionWindow)
    {
        const string httpConcurrentRequests = "10";

        var httpRule = new ContainerAppScaleRule
        {
            Name = "http-concurrency",
            Http = new ContainerAppHttpScaleRule
            {
                Metadata =
                {
                    ["concurrentRequests"] = httpConcurrentRequests
                }
            }
        };

        return instanceTier switch
        {
            InstanceTier.OnDemand => new ContainerAppScale
            {
                MinReplicas = 0,
                MaxReplicas = 1,
                Rules =
                {
                    httpRule
                }
            },
            InstanceTier.Dedicated => new ContainerAppScale
            {
                MinReplicas = 1,
                MaxReplicas = 1
            },
            InstanceTier.Scheduled => BuildScheduledScale(executionWindow, httpRule),
            _ => throw new ArgumentOutOfRangeException(nameof(instanceTier), instanceTier, "Unsupported instance tier for scaling rules.")
        };
    }

    private ContainerAppScale BuildScheduledScale(
        ScheduledExecutionWindow? executionWindow,
        ContainerAppScaleRule httpRule)
    {
        ArgumentNullException.ThrowIfNull(httpRule);

        if (executionWindow is null)
            throw new ArgumentException("ScheduledExecutionWindow is required for Scheduled instance tier.", nameof(executionWindow));

        var days = BuildCronDays(executionWindow);
        var start = $"{executionWindow.StartTime.Minute} {executionWindow.StartTime.Hour} * * {days}";
        var end = $"{executionWindow.EndTime.Minute} {executionWindow.EndTime.Hour} * * {days}";

        var cronRule = new ContainerAppScaleRule
        {
            Name = "schedule-window",
            Custom = new ContainerAppCustomScaleRule
            {
                CustomScaleRuleType = "cron",
                Metadata =
                {
                    ["timezone"] = _supersetOptions.Provisioning.KedaCronTimezone,
                    ["start"] = start,
                    ["end"] = end,
                    ["desiredReplicas"] = "1"
                }
            }
        };

        return new ContainerAppScale
        {
            MinReplicas = 0,
            MaxReplicas = 1,
            Rules =
            {
                httpRule,
                cronRule
            }
        };
    }

    private static string BuildCronDays(ScheduledExecutionWindow executionWindow)
    {
        var days = new List<int> { 1, 2, 3, 4, 5 };

        if (executionWindow.IncludeSaturdays)
            days.Add(6);
        if (executionWindow.IncludeSundays)
            days.Add(0);

        return string.Join(",", days.OrderBy(static day => day));
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
   
    


    private string BuildBootstrapCommand()
    {
        string dataSchema = SupersetSchemaPermissions.User;          // ex: schema de dados reais do tenant
        string mockedDataSchema = SupersetSchemaPermissions.Developer; // ex: schema com tabelas mocked para a equipe

        return $$"""
        set -eu

        cat <<'EOF' >/app/pythonpath/superset_config.py
        import os
        from flask_appbuilder.security.manager import AUTH_REMOTE_USER

        SECRET_KEY = os.environ["SUPERSET_SECRET_KEY"]
        SQLALCHEMY_DATABASE_URI = os.environ["SUPERSET_METADATA_DATABASE_URL"]
        METADATA_SCHEMA = os.environ.get("SUPERSET_METADATA_SCHEMA", "{{SupersetTenantConfig.MetadataSchemaPrefix}}")

        # Configurações de Banco de Dados
        SQLALCHEMY_ENGINE_OPTIONS = {
            "connect_args": {
                "options": f"-c search_path={METADATA_SCHEMA},public"
            }
        }

        # Autenticação via Remote User (Impersonation)
        AUTH_TYPE = AUTH_REMOTE_USER
        HTTP_HEADER_REMOTE_USER = os.environ.get("HTTP_HEADER_REMOTE_USER", "{{SupersetProxyHeaders.RemoteUserHeaderName}}")
        
        # Bloqueia criação automática de usuários (Não cria perfil Gamma sozinho)
        AUTH_USER_REGISTRATION = False

        # Permite incorporação em Iframe e CORS
        ENABLE_CORS = True
        CORS_OPTIONS = {
            "supports_credentials": True,
            "allow_headers": ["*"],
            "resources": ["*"],
            "origins": ["*"]
        }

        # Leitura dinâmica das origens do Frontend para o Iframe
        raw_origins = os.environ.get("ALLOWED_FRAME_ANCESTORS", "").split()

        TALISMAN_CONFIG = {
            "content_security_policy": {
                "frame-ancestors": ["'self'"] + raw_origins
            },
            "force_https": False
        }
        
        X_FRAME_OPTIONS = "ALLOWALL"
        
        # Suporte a Cookies em Iframe Cross-Origin
        SESSION_COOKIE_SAMESITE = "None"
        SESSION_COOKIE_SECURE = True

        FEATURE_FLAGS = {
            "EMBEDDED_SUPERSET": True
        }
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

                    # Script Python inline para expurgar permissões globais e criar os escopos de Schema
                    python3 - <<'PYTHON_SCRIPT'
                    import os
                    from superset.app import create_app
                    from superset.extensions import db, security_manager

                    app = create_app()
                    with app.app_context():
                        from superset.models.core import Database

                        db_name = os.environ.get("SUPERSET_TENANT_DB_NAME")
                        if not db_name:
                            raise RuntimeError("SUPERSET_TENANT_DB_NAME is required for tenant role bootstrap.")

                        database_uri = os.environ.get("DATABASE_URL") or os.environ.get("SUPERSET_METADATA_DATABASE_URL")
                        if not database_uri:
                            raise RuntimeError("DATABASE_URL or SUPERSET_METADATA_DATABASE_URL is required for tenant role bootstrap.")

                        schema_scopes = {
                            os.environ.get("SUPERSET_DATA_SCHEMA_ROLE_NAME", "Scope_Data"): os.environ.get("SUPERSET_DATA_SCHEMA", "{{dataSchema}}"),
                            os.environ.get("SUPERSET_MOCKED_SCHEMA_ROLE_NAME", "Scope_MockedData"): os.environ.get("SUPERSET_MOCKED_DATA_SCHEMA", "{{mockedDataSchema}}")
                        }

                        database = db.session.query(Database).filter_by(database_name=db_name).one_or_none()
                        if database is None:
                            database = Database(database_name=db_name, expose_in_sqllab=True)
                            db.session.add(database)

                        database.database_name = db_name
                        database.expose_in_sqllab = True
                        database.set_sqlalchemy_uri(database_uri)
                        db.session.commit()

                        security_manager.create_missing_perms()

                        database_perm = security_manager.get_database_perm(database.id, database.database_name)
                        if database_perm:
                            security_manager.add_permission_view_menu("database_access", database_perm)

                        schema_permission_names = {}
                        for schema_name in schema_scopes.values():
                            schema_perm = security_manager.get_schema_perm(database, schema_name)
                            schema_permission_names[schema_name] = schema_perm
                            if schema_perm:
                                security_manager.add_permission_view_menu("schema_access", schema_perm)

                        db.session.commit()
                        
                        # 1. Permissões Globais a REVOGAR dos perfis padrão (Alpha, sql_lab, Gamma)
                        restricted_pvms = [
                            ("can_read", "Database"),
                            ("can_write", "Database"),
                            ("can_add", "Database"),
                            ("can_delete", "Database"),
                            ("can_export", "Database"),
                            ("can_external_metadata_by_name", "Database"),
                            ("can_select_star", "Database"),
                            ("can_external_metadata", "Datasource"),
                            ("can_external_metadata_by_name", "Datasource"),
                            ("can_get_or_create_dataset", "Dataset"),
                            ("can_save", "Datasource"),
                            ("can_write", "Dataset"),
                            ("can_add", "Dataset"),
                            ("can_delete", "Dataset"),
                            ("can_refresh", "Dataset"),
                        ]

                        roles_to_clean = ["Alpha", "sql_lab", "Gamma"]

                        for r_name in roles_to_clean:
                            role = security_manager.find_role(r_name)
                            if not role:
                                continue

                            # Expurga permissões de conexão e gestão de infraestrutura
                            for action, view in restricted_pvms:
                                pvm = security_manager.find_permission_view_menu(action, view)
                                if pvm and pvm in role.permissions:
                                    role.permissions.remove(pvm)

                            # Remove o acesso irrestrito ao banco inteiro para forçar a checagem por Schema
                            db_pvm = security_manager.find_permission_view_menu("database_access", database_perm) if database_perm else None
                            if db_pvm and db_pvm in role.permissions:
                                role.permissions.remove(db_pvm)

                        # 2. Criação das Roles de Escopo de Schema para dados reais e dados mocked.
                        for role_name, schema_name in schema_scopes.items():
                            scope_role = security_manager.find_role(role_name)
                            if not scope_role:
                                scope_role = security_manager.add_role(role_name)

                            # Associa a permissão pontual de Schema
                            schema_perm = schema_permission_names.get(schema_name)
                            schema_pvm = security_manager.find_permission_view_menu("schema_access", schema_perm) if schema_perm else None
                            if schema_pvm and schema_pvm not in scope_role.permissions:
                                scope_role.permissions.append(schema_pvm)

                        db.session.commit()
                        print(">>> Bootstrapping de permissões do Tenant finalizado com sucesso!")
        PYTHON_SCRIPT

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
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _supersetOptions.Provisioning.StartupHealthCheckMaxAttempts; attempt++)
        {
            try
            {
                await _supersetApi.GetHealthAsync(new Uri(fqdnUrl), cancellationToken);
                _ = await _supersetAuthService.GetAdminToken(tenantId, cancellationToken);
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