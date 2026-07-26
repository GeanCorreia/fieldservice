namespace FieldService.Notification.Configuration;

public sealed class NotificationCacheOptions
{
    public const string SectionName = "Notification";

    public int CacheTtlDays { get; init; } = 3;
}
