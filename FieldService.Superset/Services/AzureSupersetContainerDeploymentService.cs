using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.Resources;
using FieldService.Superset.Interfaces;
using FieldService.Shared.Configuration;
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
    private readonly ISupersetContainerConfigurationService _supersetContainerConfigurationService;
    private readonly SupersetContainerAllowedOriginsCors _supersetContainerAllowedOriginsCors;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetSecretService _supersetSecretService;
    private readonly ArmClient _armClient;
    private readonly SupersetOptions _supersetOptions;
    private readonly AzureIdentityOptions _azureIdentityOptions;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly ILogger<AzureSupersetContainerDeploymentService> _logger;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly string _masterUsername;
    private readonly string _masterPassword;

    public AzureSupersetContainerDeploymentService(
        ISupersetContainerConfigurationService supersetContainerConfigurationService,
        ISupersetApi supersetApi,
        ISupersetSecretService supersetSecretService,
        IOptions<SupersetOptions> supersetOptions,
        IOptions<AzureIdentityOptions> azureIdentityOptions,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        ILogger<AzureSupersetContainerDeploymentService> logger,
        ISupersetAuthService supersetAuthService,
        SupersetContainerAllowedOriginsCors supersetContainerAllowedOriginsCors)
    {
   
        _supersetContainerConfigurationService = supersetContainerConfigurationService ?? throw new ArgumentNullException(nameof(supersetContainerConfigurationService));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetSecretService = supersetSecretService ?? throw new ArgumentNullException(nameof(supersetSecretService));
        _supersetContainerAllowedOriginsCors = supersetContainerAllowedOriginsCors ?? 
                                               throw new ArgumentNullException(nameof(supersetContainerAllowedOriginsCors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _armClient = new ArmClient(new DefaultAzureCredential());
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _masterUsername = _supersetOptions.Username;
        _masterPassword = _supersetOptions.Password;
        _azureIdentityOptions = (azureIdentityOptions ?? throw new ArgumentNullException(nameof(azureIdentityOptions))).Value;
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task<SupersetContainerDeploymentResult> CreateInstanceAsync(
        Guid tenantId,
        SupersetContainerConfiguration configuration,
        Guid connectionStringId,
        Guid secretKeyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var resourceName = SupersetContainer.ResourceName(tenantId);

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

            var resourceGroup = GetResourceGroup();
            var containerAppCollection = resourceGroup.GetContainerApps();
            var existingContainerApp = await GetContainerAppByNameAsync(containerAppCollection, resourceName, cancellationToken);

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
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
        }
    }

    private ResourceGroupResource GetResourceGroup()
    {
        var resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(
            _azureIdentityOptions.AzureSubscriptionId,
            _azureIdentityOptions.AzureResourceGroupName);

        return _armClient.GetResourceGroupResource(resourceGroupResourceId);
    }

    private static async Task<ContainerAppResource?> GetContainerAppByNameAsync(
        ContainerAppCollection containerAppCollection,
        string resourceName,
        CancellationToken cancellationToken)
    {
        await foreach (var containerApp in containerAppCollection.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (string.Equals(containerApp.Data.Name, resourceName, StringComparison.OrdinalIgnoreCase))
            {
                return containerApp;
            }
        }

        return null;
    }

    private async Task<SupersetContainerDeploymentResult> CreateSupersetContainerAsync(
        Guid tenantId,
        string resourceName,
        string secretKey,
        string connectionString,
        string tenantDatabaseName,
        SupersetContainerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var resourceGroup = GetResourceGroup();
        var containerAppCollection = resourceGroup.GetContainerApps();

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
                Scale = ((AzureContainerAppScale)_supersetContainerConfigurationService.CreateScaleConfiguration(
                        configuration)).Value,
                Containers = { container }
            }
        };

        var armOperation = await containerAppCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            resourceName,
            containerAppProps,
            cancellationToken);

        var createdContainerApp = armOperation.Value;
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