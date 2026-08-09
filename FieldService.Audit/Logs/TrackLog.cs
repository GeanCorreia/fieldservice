using Microsoft.Extensions.Logging;

namespace FiledService.Audit.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "audit:track:access", 
        Message = "Error tracking access [Request: {RequestId}] for resource {Resource} with id {ResourceId}. Error: {ErrorMessage}"
    )]
    public static partial void LogTrackAccess(
        this ILogger logger,
        LogLevel level, 
        Guid requestId,
        string resource,
        string? resourceId,
        string errorMessage);
    
    
    [LoggerMessage(
        EventName = "audit:track:persist",
        Message = "Error persisting audit changes [Request: {RequestId}]. Error: {ErrorMessage}"
    )]
    public static partial void LogTrackPersist(
        this ILogger logger,
        LogLevel level,
        Guid requestId,
        string errorMessage);
    
    
    [LoggerMessage(
        EventName = "audit:track:change",
        Message = "Error tracking change [Request: {RequestId}] for resource {Resource} with id {ResourceId}. Error: {ErrorMessage}"
    )]
    public static partial void LogTrackChange(
        this ILogger logger,
        LogLevel level,
        Guid requestId,
        string resource,
        string? resourceId,
        string errorMessage);
}