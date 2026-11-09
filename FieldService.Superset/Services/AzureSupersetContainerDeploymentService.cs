using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using FieldService.Superset.Interfaces;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;
using FieldService.Superset.Configuration;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Proxy;
using FieldService.Superset.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace FieldService.Superset.Services;

internal class AzureSupersetContainerDeploymentService : ISupersetContainerDeploymentService
{
    private readonly IAzureContainerConfigurationService _containerConfigurationService;
    private readonly SupersetContainerAllowedOriginsCors _supersetContainerAllowedOriginsCors;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetSecretService _supersetSecretService;
    private readonly IAzureContainerService _containers;
    private readonly SupersetOptions _supersetOptions;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly ILogger<AzureSupersetContainerDeploymentService> _logger;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly string _masterUsername;
    private readonly string _masterPassword;

    public AzureSupersetContainerDeploymentService(
        IAzureContainerConfigurationService containerConfigurationService,
        ISupersetApi supersetApi,
        ISupersetSecretService supersetSecretService,
        IOptions<SupersetOptions> supersetOptions,
        IAzureContainerService containers,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ILogger<AzureSupersetContainerDeploymentService> logger,
        ISupersetAuthService supersetAuthService,
        SupersetContainerAllowedOriginsCors supersetContainerAllowedOriginsCors)
    {

        _containerConfigurationService = containerConfigurationService ?? throw new ArgumentNullException(nameof(containerConfigurationService));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetSecretService = supersetSecretService ?? throw new ArgumentNullException(nameof(supersetSecretService));
        _supersetContainerAllowedOriginsCors = supersetContainerAllowedOriginsCors ??
                                               throw new ArgumentNullException(nameof(supersetContainerAllowedOriginsCors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _containers = containers ?? throw new ArgumentNullException(nameof(containers));
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _masterUsername = _supersetOptions.Username;
        _masterPassword = _supersetOptions.Password;
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task<SupersetContainerDeploymentResult> CreateInstanceAsync(
        Guid tenantId,
        ContainerConfiguration configuration,
        Guid connectionStringId,
        Guid secretKeyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var resourceName = SupersetTenant.ContainerResourceName(tenantId);

        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Could not acquire lock for tenant {tenantId}. " +
                                                $"Another instance creation process might be running.");
        }

        try
        {
            var connectionString = await _supersetSecretService.GetDatabaseConnectionString(connectionStringId, cancellationToken);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string with ID '{connectionStringId}' not found or is empty.");
            }


            var secretKey = await _supersetSecretService.GetSupersetSecretApiKey(secretKeyId, cancellationToken);

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException($"Secret key with ID '{secretKeyId}' not found or is empty.");
            }

            var existingContainerApp = await _containers.FindByNameAsync(resourceName, cancellationToken);

            if (existingContainerApp is not null)
            {
                var existingFqdnUrl = await ResolveFqdnUrlAsync(existingContainerApp, cancellationToken);
                _logger.LogInformation("Superset container resource already exists for tenant {TenantId}: {ResourceId}", tenantId, existingContainerApp.Id);
                throw new SupersetContainerResourceAlreadyExistsException(
                    tenantId,
                    existingContainerApp.Id.ToString(),
                    existingFqdnUrl);
            }

            var container = await CreateSupersetContainerAsync(
                tenantId,
                resourceName,
                secretKey,
                connectionString,
                SupersetTenant.Database(tenantId),
                configuration,
                cancellationToken);

            await WaitForSupersetReadinessAsync(container.FqdnUrl, tenantId, cancellationToken);
            _logger.LogInformation("Superset container resource created for tenant {TenantId}: {ResourceId}", tenantId, container.ResourceId);
            return container;
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, CancellationToken.None);
        }
    }

    private async Task<SupersetContainerDeploymentResult> CreateSupersetContainerAsync(
        Guid tenantId,
        string resourceName,
        string secretKey,
        string connectionString,
        string tenantDatabaseName,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var container = new ContainerAppContainer
        {
            Name = resourceName,
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
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_METADATA_SCHEMA", Value = SupersetTenant.MetadataSchemaPrefix },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_DATA_SCHEMA", Value = SupersetTenant.DataSchemaPrefix },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_MOCKED_DATA_SCHEMA", Value = SupersetTenant.MockedDataSchemaPrefix },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_SCHEMA_ACCESS_MODE", Value = "exclusive" },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_DATA_SCHEMA_ROLE_NAME", Value = "Scope_Data" },
                new ContainerAppEnvironmentVariable { Name = "SUPERSET_MOCKED_SCHEMA_ROLE_NAME", Value = "Scope_MockedData" }
            }
        };

        foreach (var envVar in _supersetContainerAllowedOriginsCors.Get())
        {
            container.Env.Add(envVar);
        }

        var createdContainerApp = await _containers.CreateAsync(
            resourceName,
            container,
            _containerConfigurationService.CreateScaleConfiguration(
                configuration, _supersetOptions.Provisioning.KedaCronTimezone),
            _supersetOptions.Provisioning.ContainerPort,
            cancellationToken);

        var resourceId = createdContainerApp.Id.ToString();
        var fqdnUrl = await ResolveFqdnUrlAsync(createdContainerApp, cancellationToken);

        return new SupersetContainerDeploymentResult(resourceId, fqdnUrl);
    }

    private string BuildBootstrapCommand()
    {
        return SupersetContainerBootstrapScript.BuildBootstrapCommand(
            metadataSchema: SupersetTenant.MetadataSchemaPrefix,
            dataSchema: SupersetTenant.DataSchemaPrefix,
            mockedDataSchema: SupersetTenant.MockedDataSchemaPrefix,
            remoteUserHeaderName: SupersetProxyHeaders.RemoteUserHeaderName,
            containerPort: _supersetOptions.Provisioning.ContainerPort,
            gunicornWorkers: _supersetOptions.Provisioning.GunicornWorkers,
            gunicornTimeoutSeconds: _supersetOptions.Provisioning.GunicornTimeoutSeconds);
    }


    private async Task<string> ResolveFqdnUrlAsync(
        ContainerAppResource containerAppResource,
        CancellationToken cancellationToken)
    {
        return await _containers.ResolveFqdnUrlAsync(
            containerAppResource,
            _supersetOptions.Provisioning.StartupHealthCheckMaxAttempts,
            TimeSpan.FromSeconds(_supersetOptions.Provisioning.StartupHealthCheckDelaySeconds),
            cancellationToken);
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(_supersetOptions.Provisioning.StartupHealthCheckDelaySeconds), cancellationToken);
        }

        throw new TimeoutException(
            $"Superset container at '{fqdnUrl}' did not become healthy after {_supersetOptions.Provisioning.StartupHealthCheckMaxAttempts} attempts.",
            lastException);
    }


}