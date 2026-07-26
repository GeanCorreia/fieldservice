using System.Security.Claims;
using System.Text.Json.Serialization;

namespace FieldService.SignalR.Types;

public sealed record SignalRConnectionContext(
    string ConnectionId,
    [property: JsonIgnore]
    ClaimsPrincipal User,
    Guid UserId,
    Guid DeviceId,
    IReadOnlyCollection<Guid> TenantIds);
