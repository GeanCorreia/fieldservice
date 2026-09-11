using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using FieldService.Observability.Types;

namespace FieldService.Authentication.Types;

public sealed record SessionActivityCacheModel(
    Guid Id,
    Guid SessionId,
    string JwtId,
    string IpAddressHash,
    string? UserAgentHash,
    DateTimeOffset Timestamp,
    RequestChannel Channel,
    Guid RequestId);
