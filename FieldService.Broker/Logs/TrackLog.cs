using Microsoft.Extensions.Logging;

namespace FieldService.Broker.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "broker:send-message:error",
        Message = "Error sending broker message with id {MessageId} to entity {EntityName}. Error: {ErrorMessage}"
    )]
    public static partial void LogSendMessage(
        this ILogger logger,
        LogLevel level,
        string messageId,
        string entityName,
        string errorMessage);
    
    [LoggerMessage(
        EventName = "broker:mark-dispatched:error",
        Message = "Error marking broker message with id {MessageId} as dispatched. Error: {ErrorMessage}"
    )]
    public static partial void LogMarkAsDispatched(
        this ILogger logger,
        LogLevel level,
        string messageId,
        string errorMessage);


    [LoggerMessage(
        EventName = "broker:mark-expired:error",
        Message = "Error marking broker message with id {MessageId} as expired. Error: {ErrorMessage}"
    )]
    public static partial void LogMarkAsExpired(
        this ILogger logger,
        LogLevel level,
        string messageId,
        string errorMessage);

    [LoggerMessage(
        EventName = "broker:outbox:error",
        Message = "Error processing broker outbox operation {Operation} for message id {MessageId}. Error: {ErrorMessage}"
    )]
    public static partial void LogOutbox(
        this ILogger logger,
        LogLevel level,
        string operation,
        string? messageId,
        string errorMessage);

    [LoggerMessage(
        EventName = "broker:send-message:batch:error",
        Message = "Error sending broker batch for message ids {MessageIds}. Error: {ErrorMessage}"
    )]
    public static partial void LogBatchSendMessage(
        this ILogger logger,
        LogLevel level,
        IEnumerable<Guid> messageIds,
        string errorMessage);

    [LoggerMessage(
        EventName = "broker:mark-expired:batch:error",
        Message = "Error marking broker batch as expired for message ids {MessageIds}. Error: {ErrorMessage}"
    )]
    public static partial void LogBatchMarkAsExpired(
        this ILogger logger,
        LogLevel level,
        IEnumerable<Guid> messageIds,
        string errorMessage);
}
