using System.Security.Claims;
using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Types;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Services;

internal sealed class AuthorizationService(
    IUserContextRepository userContextRepository,
    IAuthorizationCacheService authorizationCacheService,
    IUserAuthorizationMapper authorizationMapper) : FieldService.Authorization.Interfaces.IAuthorizationService
{
    public Task<UserAuthorizationSnapshot?> GetSnapshotAsync(AuthorizationHandlerContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated != true)
            return Task.FromResult<UserAuthorizationSnapshot?>(null);

        var (userId, tenantId) = ExtractContext(context.User);
        if (userId is null || tenantId is null)
            return Task.FromResult<UserAuthorizationSnapshot?>(null);

        return GetSnapshotAsync(userId.Value, tenantId.Value, ct);
    }

    public async Task<UserAuthorizationSnapshot?> GetSnapshotAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        ct.ThrowIfCancellationRequested();

        var cached = await authorizationCacheService.GetUserContext(userId, tenantId, ct);
        if (cached is not null)
            return cached;

        var userContext = await userContextRepository.GetUserContext(userId, tenantId, ct);
        if (userContext is null)
            return null;

        var snapshot = authorizationMapper.Map(userContext);
        await QueueCacheSave(snapshot, ct);
        return snapshot;
    }

    private async Task QueueCacheSave(
        UserAuthorizationSnapshot snapshot, 
        CancellationToken cancellationToken = default)
    {
        await authorizationCacheService.SaveUserContext(snapshot, cancellationToken);
    }

    private static (Guid? UserId, Guid? TenantId) ExtractContext(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userId = TryGetGuid(principal, ClaimTypes.NameIdentifier, "sub", "user_id", ClaimsExtensions.UserId);
        var tenantId = TryGetGuid(principal, "tenant_id", "tid", ClaimsExtensions.TenantId);
        return (userId, tenantId);
    }

    private static Guid? TryGetGuid(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (Guid.TryParse(value, out var guid))
                return guid;
        }

        return null;
    }
}
