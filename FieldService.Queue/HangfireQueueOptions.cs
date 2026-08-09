namespace FieldService.Queue;

public sealed class HangfireQueueOptions
{
    public const string SectionName = "Queue:Hangfire";

    public string ConnectionStringName { get; set; } = "Hangfire";
    public string SchemaName { get; set; } = "hangfire";
    public int QueuePollIntervalSeconds { get; set; } = 15;
    public int InvisibilityTimeoutMinutes { get; set; } = 30;
    public int DistributedLockTimeoutMinutes { get; set; } = 10;
    public int SchedulePollingIntervalSeconds { get; set; } = 15;
    public int SuccessfulJobRetentionDays { get; set; } = 7;
    public int RetryAttempts { get; set; } = 3;
    public int[] RetryDelaysInSeconds { get; set; } = [60, 300, 900];
    public int WorkerCount { get; set; }
    public string[] Queues { get; set; } = ["default"];
    public HangfireDashboardOptions Dashboard { get; set; } = new();
}

public sealed class HangfireDashboardOptions
{
    public string Path { get; set; } = "/hangfire";
    public string[] EnabledEnvironments { get; set; } = ["Development"];
    public bool AllowAnonymousInDevelopment { get; set; } = true;
    public string AuthorizationPolicy { get; set; } = "Admin";

    public bool IsEnabledForEnvironment(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName) || EnabledEnvironments.Length == 0)
            return false;

        return EnabledEnvironments.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
    }
}
