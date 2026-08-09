using FieldService.Authorization.Entities;
using FieldService.Authorization.Events;
using FieldService.Authorization.Interfaces;
using FieldService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Authorization.Data.Repositories;

internal sealed class UserContextRepository(
    AuthorizationDbContext dbContext,
    ISqlUnitOfWork<AuthorizationDbContext> unitOfWork) : IUserContextRepository
{
    public async Task<UserAuthorizationContext?> GetUserContext(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var roleEvents = await dbContext.RoleEvents
            .Where(e => e.UserId == userId && e.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var permissionEvents = await dbContext.PermissionEvents
            .Where(e => e.UserId == userId && e.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var suspensionEvents = await dbContext.SuspensionEvents
            .Where(e => e.UserId == userId && e.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        if (roleEvents.Count == 0 && permissionEvents.Count == 0 && suspensionEvents.Count == 0)
            return null;

        var contextId = GenerateContextId(userId, tenantId);

        return new UserAuthorizationContext(
            permissionEvents,
            roleEvents,
            suspensionEvents,
            contextId,
            userId,
            tenantId);
    }

    public async Task SaveUserContext(UserAuthorizationContext userContext, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        // Get current events from database
        var existingRoleEvents = await dbContext.RoleEvents
            .Where(e => e.UserId == userContext.UserId && e.TenantId == userContext.TenantId)
            .Select(e => e.Id)
            .ToHashSetAsync(ct);

        var existingPermissionEvents = await dbContext.PermissionEvents
            .Where(e => e.UserId == userContext.UserId && e.TenantId == userContext.TenantId)
            .Select(e => e.Id)
            .ToHashSetAsync(ct);

        var existingSuspensionEvents = await dbContext.SuspensionEvents
            .Where(e => e.UserId == userContext.UserId && e.TenantId == userContext.TenantId)
            .Select(e => e.Id)
            .ToHashSetAsync(ct);

        // Add new role events
        var newRoleEvents = userContext.GetRoleEvents()
            .Where(e => !existingRoleEvents.Contains(e.Id))
            .ToList();
        
        if (newRoleEvents.Count > 0)
            await dbContext.RoleEvents.AddRangeAsync(newRoleEvents, ct);

        // Add new permission events
        var newPermissionEvents = userContext.GetPermissionEvents()
            .Where(e => !existingPermissionEvents.Contains(e.Id))
            .ToList();
        
        if (newPermissionEvents.Count > 0)
            await dbContext.PermissionEvents.AddRangeAsync(newPermissionEvents, ct);

        // Add new suspension events
        var newSuspensionEvents = userContext.GetSuspensionEvents()
            .Where(e => !existingSuspensionEvents.Contains(e.Id))
            .ToList();
        
        if (newSuspensionEvents.Count > 0)
            await dbContext.SuspensionEvents.AddRangeAsync(newSuspensionEvents, ct);

        await unitOfWork.PersistChangesAsync(ct);
    }

    private static Guid GenerateContextId(Guid userId, Guid tenantId)
    {
        // Generate a deterministic ID based on userId and tenantId
        using var hash = System.Security.Cryptography.SHA256.Create();
        var combined = userId.ToString() + tenantId.ToString();
        var hashBytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hashBytes.Take(16).ToArray());
    }
}
