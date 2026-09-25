using Microsoft.Extensions.Logging;

namespace FieldService.Authentication.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "authentication:user-authorization-cache:error",
        Message = "Error caching user authorization for userId {UserId}. Details: {Exception}")]
    public static partial void LogUserAuthorizationCacheError(
        this ILogger logger,
        LogLevel level,
        Guid userId,
        Exception exception);


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
