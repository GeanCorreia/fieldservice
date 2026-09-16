using Microsoft.Extensions.Logging;

namespace FieldService.Authorization.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "authorization:cache:error",
        Message = "Error handling authorization cache. Error: {ExceptionMessage}")]
    public static partial void LogAuthorizationCache(
        this ILogger logger,
        LogLevel level,
        Exception exception,
        string exceptionMessage);

  
}