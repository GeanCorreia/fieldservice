using Microsoft.Extensions.Logging;

namespace FiledService.Audit.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "audit:track:access", 
        Message = "Error tracking access for resource {Resource} with id {ResourceId}. Error: {ErrorMessage}"
    )]
    public static partial void LogTrackAccess(
        this ILogger logger,
        LogLevel level, 
        string resource,
        string? resourceId,
        string errorMessage);
    
    
    [LoggerMessage(
        EventName = "audit:track:persist",
        Message = "Error persisting audit changes. Error: {ErrorMessage}"
    )]
    public static partial void LogTrackPersist(
        this ILogger logger,
        LogLevel level,
        string errorMessage);
    
    
    [LoggerMessage(
        EventName = "audit:track:change",
            Message = "Error tracking change for resource {Resource} with id {ResourceId}. Error: {ErrorMessage}"
    )]
    public static partial void LogTrackChange(
        this ILogger logger,
        LogLevel level,
        string resource,
        string? resourceId,
        string errorMessage);
    
    [LoggerMessage(
        EventName = "audit:session-activity-persistence:error",
        Message = "Error persisting requestId {RequestId}. Error: {ErrorMessage}")]
    public static partial void LogAuditRequestPersistenceError(
        this ILogger logger,
        LogLevel level,
        Guid requestId,
        string errorMessage);

    [LoggerMessage(
        EventName = "audit:change-persistence:error",
        Message = "Error persisting changeId {ChangeId}. Error: {ErrorMessage}")]
    public static partial void LogAuditChangePersistenceError(
        this ILogger logger,
        LogLevel level,
        Guid changeId,
        string errorMessage);
}