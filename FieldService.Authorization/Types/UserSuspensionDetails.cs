using FieldService.Authorization.Events;
using FieldService.Shared.Types;
namespace FieldService.Authorization.Types;

public record UserSuspensionDetails(
    Guid UserId,
    Guid TenantId,
    SuspensionSource Source,
    Interval Interval);