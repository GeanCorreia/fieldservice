using FieldService.Authentication.Entities;

namespace FieldService.Authentication.Types;

public sealed record SessionCacheModel(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    string ExternalId,
    AuthenticationProvider Provider,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    RevocationReason? RevocationReason);
