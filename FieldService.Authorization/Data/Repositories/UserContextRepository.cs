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
    public async Task<IEnumerable<UserAuthorization>> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
      

        var roleEvents = await dbContext.RoleEvents
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var permissionEvents = await dbContext.PermissionEvents
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var suspensionEvents = await dbContext.SuspensionEvents
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        if (roleEvents.Count == 0 && permissionEvents.Count == 0 && suspensionEvents.Count == 0)
            return null;

        var contextId = GenerateContextId(userId);

        var tenantIds = roleEvents.Select(e => e.TenantId)
            .Concat(permissionEvents.Select(e => e.TenantId))
            .Concat(suspensionEvents.Select(e => e.TenantId))
            .Distinct()
            .ToList();

        var userContexts = new List<UserAuthorization>();

        foreach (var tenantId in tenantIds)
        {
            var tenantRoleEvents = roleEvents
                .Where(e => e.TenantId == tenantId)
                .ToList();
            
            if (tenantRoleEvents.Count == 0)
                continue;

            var tenantPermissionEvents = permissionEvents
                .Where(e => e.TenantId == tenantId)
                .ToList();

            var tenantSuspensionEvents = suspensionEvents
                .Where(e => e.TenantId == tenantId)
                .ToList();

            userContexts.Add(new UserAuthorization(
                tenantPermissionEvents,
                tenantRoleEvents,
                tenantSuspensionEvents,
                contextId,
                userId,
                tenantId));
        }

        return userContexts.Count == 0 ? null : userContexts;
    }

    public async Task SaveUserContext(UserAuthorization user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        
        var existingRoleEvents = await dbContext.RoleEvents
            .Where(e => e.UserId == user.UserId && e.TenantId == user.TenantId)
            .Select(e => e.Id)
            .ToHashSetAsync(ct);

        var existingPermissionEvents = await dbContext.PermissionEvents
            .Where(e => e.UserId == user.UserId && e.TenantId == user.TenantId)
            .Select(e => e.Id)
            .ToHashSetAsync(ct);

        var existingSuspensionEvents = await dbContext.SuspensionEvents
            .Where(e => e.UserId == user.UserId && e.TenantId == user.TenantId)
            .Select(e => e.Id)
            .ToHashSetAsync(ct);
        
        var newRoleEvents = user.GetRoleEvents()
            .Where(e => !existingRoleEvents.Contains(e.Id))
            .ToList();
        
        if (newRoleEvents.Count > 0)
            await dbContext.RoleEvents.AddRangeAsync(newRoleEvents, ct);
        
        var newPermissionEvents = user.GetPermissionEvents()
            .Where(e => !existingPermissionEvents.Contains(e.Id))
            .ToList();
        
        if (newPermissionEvents.Count > 0)
            await dbContext.PermissionEvents.AddRangeAsync(newPermissionEvents, ct);
        
        var newSuspensionEvents = user.GetSuspensionEvents()
            .Where(e => !existingSuspensionEvents.Contains(e.Id))
            .ToList();
        
        if (newSuspensionEvents.Count > 0)
            await dbContext.SuspensionEvents.AddRangeAsync(newSuspensionEvents, ct);

        await unitOfWork.PersistChangesAsync(ct);
    }

    private static Guid GenerateContextId(Guid userId)
    {

        using var hash = System.Security.Cryptography.SHA256.Create();
        var combined = userId.ToString();
        var hashBytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hashBytes.Take(16).ToArray());
    }
}
