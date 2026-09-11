namespace FieldService.Broker.Configuration;

public sealed class BrokerOutboxOptions
{
    public const string SectionName = "BrokerOutbox";
    public string RetryJobCron { get; init; } = "*/1 * * * *";
    public int PendingRetryIntervalMinutes { get; init; } = 5;
    public int MaxRetryCount { get; init; } = 100;
    
    public int MessageLockTtlSeconds { get; init; } = 30;
}
