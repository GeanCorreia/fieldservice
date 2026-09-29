using FieldService.Superset.Entities;

namespace FieldService.Superset.Dtos;

public record SupersetTenantCreateParams(
    Guid UserId,
    Guid TenantId,
    InstanceTier InstanceTier,
    ScheduledExecutionWindow? ScheduledExecutionWindow = null);

internal sealed record SupersetDatabaseParams(
    string DatabaseName,
    string Username,
    string Password
);


internal sealed record SupersetContainerCreationResult(
    string ResourceId,
    string FqdnUrl
);


// internal record SupersetTenantCreateParams(
//     Guid UserId,
//     Guid TenantId,
//     InstanceTier InstanceTier,
//     ScheduledExecutionWindow? ScheduledExecutionWindow = null);
