using FieldService.Shared.Types;

namespace FieldService.Authorization.Types;



public record HistoryUserAuthorizationContext(
    Guid UserId,
    Guid TenantId,
    Dictionary<Role,IEnumerable<Interval>>Role,
    Dictionary<Permission,IEnumerable<Interval>>Permission,
    IEnumerable<UserSuspensionDetails>Suspension);