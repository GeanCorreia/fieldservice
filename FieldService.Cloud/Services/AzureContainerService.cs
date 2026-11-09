using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.Resources;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;
using FieldService.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace FieldService.Cloud.Services;

public sealed class AzureContainerService(
    ArmClient armClient,
    IOptions<AzureIdentityOptions> azureIdentityOptions) : IAzureContainerService
{
    private readonly ArmClient _armClient = armClient ?? throw new ArgumentNullException(nameof(armClient));
    private readonly AzureIdentityOptions _options =
        (azureIdentityOptions ?? throw new ArgumentNullException(nameof(azureIdentityOptions))).Value;

    public string ResourceId(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return ContainerAppResource.CreateResourceIdentifier(
            _options.AzureSubscriptionId, _options.AzureResourceGroupName, resourceName).ToString();
    }

    public async Task<ContainerAppResource> GetAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        var resource = _armClient.GetContainerAppResource(new ResourceIdentifier(resourceId));
        return (await resource.GetAsync(cancellationToken)).Value;
    }

    public async Task<ContainerAppResource?> FindByNameAsync(
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        try
        {
            return await GetAsync(ResourceId(resourceName), cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<ContainerAppResource> CreateAsync(
        string resourceName,
        ContainerAppContainer container,
        ContainerAppScale scale,
        int targetPort,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(scale);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetPort);

        var data = new ContainerAppData(_options.AzureLocation)
        {
            EnvironmentId = new ResourceIdentifier(_options.AzureContainerAppEnvironmentId),
            Configuration = new ContainerAppConfiguration
            {
                Ingress = new ContainerAppIngressConfiguration
                {
                    External = true,
                    TargetPort = targetPort,
                    Transport = ContainerAppIngressTransportMethod.Auto
                }
            },
            Template = new ContainerAppTemplate
            {
                Scale = scale,
                Containers = { container }
            }
        };
        var groupId = ResourceGroupResource.CreateResourceIdentifier(
            _options.AzureSubscriptionId, _options.AzureResourceGroupName);
        var collection = _armClient.GetResourceGroupResource(groupId).GetContainerApps();
        return (await collection.CreateOrUpdateAsync(
            WaitUntil.Completed, resourceName, data, cancellationToken)).Value;
    }

    public async Task UpdateAsync(ContainerAppResource container, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(container);
        await container.UpdateAsync(WaitUntil.Completed, container.Data, cancellationToken);
    }

    public async Task<string> ResolveFqdnUrlAsync(
        ContainerAppResource container,
        int maxAttempts,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var response = await container.GetAsync(cancellationToken);
            var fqdn = response.Value.Data.Configuration?.Ingress?.Fqdn;
            if (!string.IsNullOrWhiteSpace(fqdn))
            {
                return fqdn.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                       fqdn.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? fqdn
                    : $"https://{fqdn}";
            }

            if (attempt < maxAttempts)
                await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for Container App '{container.Id}' ingress FQDN.");
    }

    public async Task<ContainerInstanceStatus> GetStatusAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var container = await GetAsync(resourceId, cancellationToken);
        return MapStatus(container.Data);
    }

    public async Task ApplyExecutionScaleAsync(
        string resourceId,
        ExecutionType executionType,
        bool isScaleUp,
        CancellationToken cancellationToken = default)
    {
        var container = await GetAsync(resourceId, cancellationToken);
        var target = ResolveScaleTarget(executionType, isScaleUp);
        container.Data.Template ??= new ContainerAppTemplate();
        container.Data.Template.Scale ??= new ContainerAppScale();
        container.Data.Template.Scale.MinReplicas = target.MinReplicas;
        container.Data.Template.Scale.MaxReplicas = target.MaxReplicas;
        await UpdateAsync(container, cancellationToken);
    }

    public static ContainerInstanceStatus MapStatus(ContainerAppData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var minReplicas = data.Template?.Scale?.MinReplicas ?? 0;
        var state = (data.ProvisioningState.ToString() ?? string.Empty)
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return state switch
        {
            "failed" => ContainerInstanceStatus.Failed,
            "succeeded" => minReplicas > 0 ? ContainerInstanceStatus.Running : ContainerInstanceStatus.Stopped,
            "provisioning" or "inprogress" or "pending" or "accepted" or "creating" or "updating" or "waiting"
                => ContainerInstanceStatus.Provisioning,
            "deleting" => ContainerInstanceStatus.Deleting,
            "canceled" => ContainerInstanceStatus.Canceled,
            _ => minReplicas > 0 ? ContainerInstanceStatus.Provisioning : ContainerInstanceStatus.Unknown
        };
    }

    public static (int MinReplicas, int MaxReplicas) ResolveScaleTarget(ExecutionType executionType, bool isScaleUp)
    {
        return executionType switch
        {
            ExecutionType.OnDemand or ExecutionType.Scheduled => (0, 1),
            ExecutionType.AlwaysOn => (1, 1),
            _ => isScaleUp ? (1, 1) : (0, 1)
        };
    }
}
