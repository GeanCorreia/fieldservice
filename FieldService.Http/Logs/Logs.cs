using Microsoft.Extensions.Logging;

namespace FieldService.Http.Logs;

internal static partial class Logs
{
    [LoggerMessage(
        EventName = "authentication:get-user-tenants:error",
        Message = "Error handling GetUserTenantsQuery. Error: {ErrorMessage}")]
    public static partial void LogGetUserTenantsQuery(
        this ILogger logger,
        LogLevel level,
        string errorMessage);

    [LoggerMessage(
        EventName = "authentication:get-user-tenants:inconsistent-users",
        Message = "Inconsistent UserIds returned while handling GetUserTenantsQuery.")]
    public static partial void LogGetUserTenantsInconsistentUsers(
        this ILogger logger,
        LogLevel level);
}
