using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using FieldService.Shared.Configuration;
using FieldService.Superset.Configuration;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Services;

internal sealed class AzureContainerAppScale : ICloudContainerAppScale
{
    public ContainerAppScale Value { get; }

    public AzureContainerAppScale(ContainerAppScale value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
internal class AzureSupersetContainerConfigurationService : ISupersetContainerConfigurationService
{
    private readonly ArmClient _armClient;
    private readonly SupersetOptions _supersetOptions;
    private readonly AzureIdentityOptions _azureIdentityOptions;

    public AzureSupersetContainerConfigurationService(
        IOptions<SupersetOptions> supersetOptions,
        IOptions<AzureIdentityOptions> azureIdentityOptions)
    {
        _supersetOptions = supersetOptions.Value ?? throw new ArgumentNullException(nameof(supersetOptions));
        _azureIdentityOptions = azureIdentityOptions.Value ?? throw new ArgumentNullException(nameof(azureIdentityOptions));
        _armClient = new ArmClient(new DefaultAzureCredential());
    }
    
    public string CloudResourceId(Guid containerId) => 
        $"/subscriptions/{_azureIdentityOptions.AzureSubscriptionId}/resourceGroups/{_azureIdentityOptions.AzureResourceGroupName}/providers/Microsoft.App/containerApps/{SupersetContainer.ResourceName(containerId)}";
    
    public async Task ApplyContainerConfigurationAsync(
        SupersetContainer container,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(container);
        
        if(container.ProviderType != ProviderType.Azure)
            throw new InvalidOperationException($"Invalid provider type: {container.ProviderType}. Expected: {ProviderType.Azure}.");

        if (string.IsNullOrWhiteSpace(container.ResourceId))
            throw new ResourceIdNotFoundException(container.ResourceId);
        

        try
        {
            var azureResourceId = new ResourceIdentifier(CloudResourceId(container.Id));
            var containerAppResource = _armClient.GetContainerAppResource(azureResourceId);
            var response = await containerAppResource.GetAsync(cancellationToken);
            var containerApp = response.Value;

            containerApp.Data.Template ??= new ContainerAppTemplate();
            containerApp.Data.Template.Scale = ((AzureContainerAppScale)CreateScaleConfiguration(
                container.Configuration)).Value;

            await containerAppResource.UpdateAsync(WaitUntil.Completed, containerApp.Data, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new ResourceIdNotFoundException(container.ResourceId);
        }
    }

    public ICloudContainerAppScale CreateScaleConfiguration(
        SupersetContainerConfiguration containerConfig,
        CancellationToken cancellationToken = default)
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

        var scale = containerConfig.ExecutionType switch
        {
            ExecutionType.OnDemand => new AzureContainerAppScale(new ContainerAppScale
            {
                MinReplicas = 0,
                MaxReplicas = 1,
                Rules =
                {
                    httpRule
                }
            }),

            ExecutionType.AlwaysOn => new AzureContainerAppScale(new ContainerAppScale
            {
                MinReplicas = 1,
                MaxReplicas = containerConfig.MaxReplicas,
            }),

            ExecutionType.Scheduled =>
                BuildScheduledScale(
                    containerConfig.MinReplicas, 
                    containerConfig.MaxReplicas, 
                    containerConfig.ExecutionWindow, 
                    httpRule),

            _ => throw new ArgumentOutOfRangeException(
                nameof(containerConfig.ExecutionType),
                containerConfig.ExecutionType,
                "Unsupported execution type for scaling rules.")
        };

        return scale;
    }
    

    private ICloudContainerAppScale BuildScheduledScale(
        int minReplicas,
        int maxReplicas,
        ExecutionWindow? executionWindow,
        ContainerAppScaleRule httpRule)
    {
        ArgumentNullException.ThrowIfNull(httpRule);
        ArgumentNullException.ThrowIfNull(executionWindow);

        var scale = new ContainerAppScale
        {
            MinReplicas = minReplicas,
            MaxReplicas = maxReplicas,
            Rules =
            {
                httpRule
            }
        };

        AddCronRule(
            scale,
            "monday",
            DayOfWeek.Monday,
            executionWindow.Monday);

        AddCronRule(
            scale,
            "tuesday",
            DayOfWeek.Tuesday,
            executionWindow.Tuesday);

        AddCronRule(
            scale,
            "wednesday",
            DayOfWeek.Wednesday,
            executionWindow.Wednesday);

        AddCronRule(
            scale,
            "thursday",
            DayOfWeek.Thursday,
            executionWindow.Thursday);

        AddCronRule(
            scale,
            "friday",
            DayOfWeek.Friday,
            executionWindow.Friday);

        AddCronRule(
            scale,
            "saturday",
            DayOfWeek.Saturday,
            executionWindow.Saturday);

        AddCronRule(
            scale,
            "sunday",
            DayOfWeek.Sunday,
            executionWindow.Sunday);

        return new AzureContainerAppScale(scale);
    }

    private void AddCronRule(
        ContainerAppScale scale,
        string dayName,
        DayOfWeek dayOfWeek,
        DailyExecutionWindow? window)
    {
        if (window is null)
            return;

        var day = dayOfWeek switch
        {
            DayOfWeek.Sunday => 0,
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek))
        };

        var start = $"{window.StartTime.Minute} {window.StartTime.Hour} * * {day}";
        var end = $"{window.EndTime.Minute} {window.EndTime.Hour} * * {day}";

        scale.Rules.Add(new ContainerAppScaleRule
        {
            Name = $"schedule-{dayName}",
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
        });
    }
}


