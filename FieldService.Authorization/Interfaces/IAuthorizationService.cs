using Microsoft.AspNetCore.Authorization;
using FieldService.Authorization.Types;

namespace FieldService.Authorization.Interfaces;

public interface IAuthorizationService
{
    Task<UserAuthorizationSnapshot?> GetSnapshotAsync(AuthorizationHandlerContext context, CancellationToken ct = default);
    Task<UserAuthorizationSnapshot?> GetSnapshotAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
}
