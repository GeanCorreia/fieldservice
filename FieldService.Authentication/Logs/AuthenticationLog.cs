using Microsoft.Extensions.Logging;

namespace FieldService.Authentication.Logs;

internal static partial class Logs
{


    [LoggerMessage(
        EventName = "authentication:session-persistence:error",
        Message = "Error persisting inactive sessions. Error: {ErrorMessage}")]
    public static partial void LogSessionPersistenceError(
        this ILogger logger,
        LogLevel level,
        string errorMessage);
    
    [LoggerMessage(
        EventName = "authentication:update-session-expiration:error",
        Message = "Error updating sessionId {SessionId} expiration. Error: {ErrorMessage}")]
    public static partial void LogSessionUpdateExpirationError(
        this ILogger logger,
        LogLevel level,
        Guid sessionId,
        string errorMessage);
}
