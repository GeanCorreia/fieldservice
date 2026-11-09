using Azure.ResourceManager.AppContainers.Models;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Interfaces;

namespace FieldService.Cloud.Services;

public sealed class AzureContainerConfigurationService : IAzureContainerConfigurationService
{
    private readonly IAzureContainerService _containers;

    public AzureContainerConfigurationService(IAzureContainerService containers)
    {
        _containers = containers ?? throw new ArgumentNullException(nameof(containers));
    }

    public async Task ApplyAsync(
        string resourceId,
        ContainerConfiguration configuration,
        string cronTimezone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var containerApp = await _containers.GetAsync(resourceId, cancellationToken);

        var desiredScale = CreateScaleConfiguration(configuration, cronTimezone);
        if (IsSameScale(containerApp.Data.Template?.Scale, desiredScale))
            return;

        containerApp.Data.Template ??= new ContainerAppTemplate();
        containerApp.Data.Template.Scale = desiredScale;

        await _containers.UpdateAsync(containerApp, cancellationToken);
    }

    private static bool IsSameScale(ContainerAppScale? current, ContainerAppScale desired)
    {
        if (current is null)
            return false;

        if (current.MinReplicas != desired.MinReplicas || current.MaxReplicas != desired.MaxReplicas)
            return false;

        var currentRules = current.Rules.OrderBy(r => r.Name, StringComparer.Ordinal).ToArray();
        var desiredRules = desired.Rules.OrderBy(r => r.Name, StringComparer.Ordinal).ToArray();

        if (currentRules.Length != desiredRules.Length)
            return false;

        for (var i = 0; i < currentRules.Length; i++)
        {
            if (!IsSameRule(currentRules[i], desiredRules[i]))
                return false;
        }

        return true;
    }

    private static bool IsSameRule(ContainerAppScaleRule current, ContainerAppScaleRule desired)
    {
        if (!string.Equals(current.Name, desired.Name, StringComparison.Ordinal))
            return false;

        if (!IsSameHttpRule(current.Http, desired.Http))
            return false;

        return IsSameCustomRule(current.Custom, desired.Custom);
    }

    private static bool IsSameHttpRule(ContainerAppHttpScaleRule? current, ContainerAppHttpScaleRule? desired)
    {
        if (current is null || desired is null)
            return current is null && desired is null;

        return IsSameMetadata(current.Metadata, desired.Metadata);
    }

    private static bool IsSameCustomRule(ContainerAppCustomScaleRule? current, ContainerAppCustomScaleRule? desired)
    {
        if (current is null || desired is null)
            return current is null && desired is null;

        if (!string.Equals(current.CustomScaleRuleType, desired.CustomScaleRuleType, StringComparison.Ordinal))
            return false;

        return IsSameMetadata(current.Metadata, desired.Metadata);
    }

    private static bool IsSameMetadata(
        IDictionary<string, string> current,
        IDictionary<string, string> desired)
    {
        if (current.Count != desired.Count)
            return false;

        foreach (var (key, value) in current)
        {
            if (!desired.TryGetValue(key, out var desiredValue))
                return false;

            if (!string.Equals(value, desiredValue, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    public ContainerAppScale CreateScaleConfiguration(
        ContainerConfiguration containerConfig,
        string cronTimezone)
    {
        ArgumentNullException.ThrowIfNull(containerConfig);
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
            ExecutionType.OnDemand => new ContainerAppScale
            {
                MinReplicas = 0,
                MaxReplicas = 1,
                Rules =
                {
                    httpRule
                }
            },

            ExecutionType.AlwaysOn => new ContainerAppScale
            {
                MinReplicas = 1,
                MaxReplicas = containerConfig.MaxReplicas,
            },

            ExecutionType.Scheduled =>
                BuildScheduledScale(
                    containerConfig.MinReplicas,
                    containerConfig.MaxReplicas,
                    containerConfig.ExecutionWindow,
                    httpRule,
                    cronTimezone),

            _ => throw new ArgumentOutOfRangeException(
                nameof(containerConfig.ExecutionType),
                containerConfig.ExecutionType,
                "Unsupported execution type for scaling rules.")
        };

        return scale;
    }


    private static ContainerAppScale BuildScheduledScale(
        int minReplicas,
        int maxReplicas,
        ExecutionWindow? executionWindow,
        ContainerAppScaleRule httpRule,
        string cronTimezone)
    {
        ArgumentNullException.ThrowIfNull(httpRule);
        ArgumentNullException.ThrowIfNull(executionWindow);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronTimezone);

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
            executionWindow.Monday, cronTimezone);

        AddCronRule(
            scale,
            "tuesday",
            DayOfWeek.Tuesday,
            executionWindow.Tuesday, cronTimezone);

        AddCronRule(
            scale,
            "wednesday",
            DayOfWeek.Wednesday,
            executionWindow.Wednesday, cronTimezone);

        AddCronRule(
            scale,
            "thursday",
            DayOfWeek.Thursday,
            executionWindow.Thursday, cronTimezone);

        AddCronRule(
            scale,
            "friday",
            DayOfWeek.Friday,
            executionWindow.Friday, cronTimezone);

        AddCronRule(
            scale,
            "saturday",
            DayOfWeek.Saturday,
            executionWindow.Saturday, cronTimezone);

        AddCronRule(
            scale,
            "sunday",
            DayOfWeek.Sunday,
            executionWindow.Sunday, cronTimezone);

        return scale;
    }

    private static void AddCronRule(
        ContainerAppScale scale,
        string dayName,
        DayOfWeek dayOfWeek,
        DailyExecutionWindow? window,
        string cronTimezone)
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
                    ["timezone"] = cronTimezone,
                    ["start"] = start,
                    ["end"] = end,
                    ["desiredReplicas"] = "1"
                }
            }
        });
    }
}
