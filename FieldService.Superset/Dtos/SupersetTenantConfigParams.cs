using FieldService.Superset.Entities;

namespace FieldService.Superset.Dtos;

internal record SupersetTenantConfigParams(
    Guid TenantId,
    InstanceTier InstanceTier,
    Guid SupersetSecretKeyId,
    Guid ConnectionStringId,
    string FqdnUrl,
    string ResourceId,
    ScheduledExecutionWindow? ScheduledExecutionWindow = null);


internal record SupersetTenantCreateParams(
    Guid UserId,
    Guid TenantId,
    InstanceTier InstanceTier,
    ScheduledExecutionWindow? ScheduledExecutionWindow = null);
