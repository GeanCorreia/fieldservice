using Microsoft.Extensions.Logging;

namespace FieldService.Notification.Logs;

internal static partial class NotificationLogs
{
    [LoggerMessage(
        EventName = "notification:provider:unavailable",
        Message = "Notification provider is unavailable. Error: {ErrorMessage}")]
    public static partial void LogNotificationProviderUnavailable(
        this ILogger logger,
        LogLevel level,
        string errorMessage);
}