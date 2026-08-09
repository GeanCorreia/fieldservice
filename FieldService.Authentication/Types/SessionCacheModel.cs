using FieldService.Authentication.Entities;

namespace FieldService.Authentication.Types;

public sealed record SessionCacheModel(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    string ExternalId,
    AuthenticationProvider Provider,
    DateTime StartedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    RevocationReason? RevocationReason,
    DateTime? LastActivityAt,
    IReadOnlyCollection<SessionActivityCacheModel> Activities);
