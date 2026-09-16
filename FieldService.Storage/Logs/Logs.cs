using FieldService.Storage.Entities;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "storage:provider-upload:error",
        Message = "Storage provider '{ProviderName}' failed to upload file with id {FileId}. Error: {ErrorMessage}"
    )]
    public static partial void LogProviderUploadError(
        this ILogger logger,
        LogLevel level,
        string providerName,
        string fileId,
        string errorMessage,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:provider-delete:error",
        Message = "Storage provider '{ProviderName}' failed to delete file with id {FileId}."
    )]
    public static partial void LogProviderDeleteError(
        this ILogger logger,
        LogLevel level,
        string providerName,
        Guid fileId,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:provider-has-uploaded:error",
        Message = "FileIds {FileIds}."
    )]
    public static partial void LogProviderHasUploadedError(
        this ILogger logger,
        LogLevel level,
        StorageProvider provider,
        IEnumerable<Guid> fileIds,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:fallback-upload:error",
        Message = "Failed to create fallback file with id {FileId}. Error: {ErrorMessage}"
    )]
    public static partial void LogFallbackUploadError(
        this ILogger logger,
        LogLevel level,
        string fileId,
        string errorMessage,
        Exception? exception = null);
    
    
    
    [LoggerMessage(
        EventName = "storage:outbox-service:error",
        Message = "Outbox service failed for file with path {Path} using provider {Provider}. Error: {ErrorMessage}"
    )]
    public static partial void LogOutboxServiceError(
        this ILogger logger,
        LogLevel level,
        string path,
        StorageProvider provider,
        string errorMessage,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:successful-upload-outbox-service:error",
        Message = "Outbox service succeeded for file with id {FileId}."
    )]
    public static partial void LogSuccessfulUploadOutboxServiceError(
        this ILogger logger,
        LogLevel level,
        Guid fileId,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:successful-upload-outbox-service:error",
        Message = "Outbox service failed for file with id {FileId}."
    )]
    public static partial void LogFailedUploadOutboxServiceError(
        this ILogger logger,
        LogLevel level,
        Guid fileId,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:corrupted-upload-outbox-service:error",
        Message = "Outbox service failed for file with id {FileId}."
    )]
    public static partial void LogCorruptedUploadOutboxServiceError(
        this ILogger logger,
        LogLevel level,
        Guid fileId,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "storage:canceled-upload-outbox-service:error",
        Message = "Outbox service failed for file with id {FileId}."
    )]
    public static partial void LogCanceledUploadOutboxServiceError(
        this ILogger logger,
        LogLevel level,
        Guid fileId,
        Exception? exception = null);
    
   
}