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
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Jobs;
using FieldService.Superset.Proxy;
using FieldService.Superset.Utils;
using Microsoft.Extensions.Configuration;
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
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ArmClient _armClient;
    private readonly SupersetOptions _supersetOptions;
    private readonly AzureIdentityOptions _azureIdentityOptions;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly PersistSupersetTenantConfigProducerWithRequest _persistSupersetTenantConfigProducerWithRequest;
    private readonly ILogger<AzureSupersetContainerDeploymentService> _logger;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly string _masterUsername;
    private readonly string _masterPassword;

    public AzureSupersetContainerDeploymentService(
        ISupersetContainerConfigurationService supersetContainerConfigurationService,
        ISupersetApi supersetApi,
        ISupersetSecretService supersetSecretService,
        ISupersetTenantService supersetTenantService,
        IOptions<SupersetOptions> supersetOptions,
        IOptions<AzureIdentityOptions> azureIdentityOptions,
        IConfiguration configuration,
        ISupersetTenantInstanceProcessingLock supersetInstanceLock,
        PersistSupersetTenantConfigProducerWithRequest persistSupersetTenantConfigProducerWithRequest,
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
        _persistSupersetTenantConfigProducerWithRequest = persistSupersetTenantConfigProducerWithRequest ?? throw new ArgumentNullException(nameof(persistSupersetTenantConfigProducerWithRequest));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _armClient = new ArmClient(new DefaultAzureCredential());
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _masterUsername = _supersetOptions.Username;
        _masterPassword = _supersetOptions.Password;
        _azureIdentityOptions = (azureIdentityOptions ?? throw new ArgumentNullException(nameof(azureIdentityOptions))).Value;
        _supersetInstanceLock = supersetInstanceLock ?? throw new ArgumentNullException(nameof(supersetInstanceLock));
    }

    public async Task<SupersetContainer> CreateInstanceAsync(
        Guid tenantId,
        SupersetTenantCreateParams supersetTenantCreateParams,
        Guid connectionStringId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supersetTenantCreateParams);

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
            
            var secretKey = await _supersetSecretService
                .CreateSupersetSecretApiKey(
                    tenantId, 
                    userId, 
                    cancellationToken);

            var container = await CreateSupersetContainer(
                tenantId,
                secretKey.KeyId,
                secretKey.Key,
                connectionString,
                SupersetTenant.Database(tenantId),
                supersetTenantCreateParams.configuration,
                cancellationToken);

            await WaitForSupersetReadinessAsync(container.FqdnUrl, tenantId, cancellationToken);
            return container;
        }
        finally
        {
            await _supersetInstanceLock.ReleaseLock(tenantId, cancellationToken);
        }
    }

    private async Task<SupersetContainer> CreateSupersetContainer(
    Guid tenantId,
    Guid secretKeyId,
    string secretKey,
    string connectionString,
    string tenantDatabaseName,
    SupersetContainerConfiguration configuration,
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
        var containerId = Guid.NewGuid();
        
        
        var container = new ContainerAppContainer
        {
            Name = SupersetContainer.ResourceName(containerId),
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
            SupersetContainer.ResourceName(containerId),
            containerAppProps,
            cancellationToken);

        var createdContainerApp = armOperation.Value;
        var resourceId = createdContainerApp.Id.ToString();
        var fqdnUrl = await ResolveFqdnUrlAsync(createdContainerApp, cancellationToken);

        return new SupersetContainer(
            id: containerId,
            tenantId: tenantId,
            providerType: ProviderType.Azure,
            secretKeyId: secretKeyId,
            resourceId: resourceId,
            fqdnUrl: fqdnUrl,
            executionType: configuration.ExecutionType,
            maxReplicas: configuration.MaxReplicas,
            minReplicas: configuration.MinReplicas,
            status: SupersetContainerStatus.Active,
            executionWindow: configuration.ExecutionWindow);;
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