using FieldService.Superset.Dtos;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "superset:tenant-instance-scale-up-container:error",
        Message = "Failed to scale up container for tenant {TenantId}. Error: {ErrorMessage}"
    )]
    public static partial void LogSupersetTenantInstanceScaleUpContainerError(
        this ILogger logger,
        LogLevel level,
        Guid tenantId,
        string errorMessage,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "superset:tenant-instance-scale-down-container:error",
        Message = "Failed to scale down container {ResourceId} for tenant {TenantId}. Error: {ErrorMessage}"
    )]
    public static partial void LogSupersetTenantInstanceScaleDownContainerError(
        this ILogger logger,
        LogLevel level,
        Guid tenantId,
        string resourceId,
        string errorMessage,
        Exception? exception = null);
    
    [LoggerMessage(
        EventName = "superset:tenant-config-create:error",
        Message = "Failed to create Superset tenant config for tenant {TenantId}. Error: {ErrorMessage}"
    )]
    public static partial void LogSupersetTenantConfigCreateError(
        this ILogger logger,
        LogLevel level,
        Guid tenantId,
        string resourceId,
        SupersetTenantConfigParams configParams,
        string errorMessage,
        Exception? exception = null);
}