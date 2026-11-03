using FieldService.Authorization.Entities;
using FieldService.Authorization.Interfaces;

namespace FieldService.Authorization.Cqrs.Command;

internal static class UserAuthorizationCommandHelpers
{
    public static async Task<UserAuthorization> LoadForTenantOrThrowAsync(
        IUserContextRepository userContextRepository,
        Guid userId,
        Guid tenantId,
        CancellationToken ct)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var contexts = await userContextRepository.GetUserAsync(userId, ct);
        var context = contexts.FirstOrDefault(x => x.TenantId == tenantId);

        if (context is null)
            throw new InvalidOperationException("User authorization context was not found for the requested tenant.");

        return context;
    }

    public static Guid ResolveCreatedBy(Guid? createdBy, Guid userId)
        => createdBy is { } value && value != default ? value : userId;
}

