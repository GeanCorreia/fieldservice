using FieldService.Superset.Entities;

namespace FieldService.Superset.Dtos;

internal record SupersetTenantConfigParams(
    Guid TenantId,
    InstanceTier InstanceTier,
    string EncryptedSupersetSecretKey,
    string EncryptedConnectionString,
    string FqdnUrl,
    string ResourceId,
    ScheduledExecutionWindow? ScheduledExecutionWindow = null);


internal record SupersetTenantCreateParams(
    Guid TenantId,
    InstanceTier InstanceTier,
    ScheduledExecutionWindow? ScheduledExecutionWindow = null);
