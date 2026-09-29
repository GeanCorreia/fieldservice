    using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using FieldService.Superset.Configuration;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Services;

internal class SupersetTenantContainerConfigurationService : ISupersetTenantContainerConfigurationService
{
    private readonly ArmClient _armClient;
    private readonly SupersetOptions _supersetOptions;

    public SupersetTenantContainerConfigurationService(IOptions<SupersetOptions> supersetOptions)
    {
        _supersetOptions = (supersetOptions ?? throw new ArgumentNullException(nameof(supersetOptions))).Value;
        _armClient = new ArmClient(new DefaultAzureCredential());
    }

    public async Task ApplyContainerConfigurationAsync(
        SupersetTenantConfig tenantConfig,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantConfig);

        if (string.IsNullOrWhiteSpace(tenantConfig.ResourceId))
            throw new ResourceIdNotFoundException(tenantConfig.TenantId, tenantConfig.ResourceId);

        try
        {
            var resourceId = new ResourceIdentifier(tenantConfig.ResourceId);
            var containerAppResource = _armClient.GetContainerAppResource(resourceId);
            var response = await containerAppResource.GetAsync(cancellationToken);
            var containerApp = response.Value;

            containerApp.Data.Template ??= new ContainerAppTemplate();
            containerApp.Data.Template.Scale = BuildScaleConfiguration(tenantConfig.InstanceTier, tenantConfig.ExecutionWindow);

            await containerAppResource.UpdateAsync(WaitUntil.Completed, containerApp.Data, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new ResourceIdNotFoundException(tenantConfig.TenantId, tenantConfig.ResourceId);
        }
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
}


