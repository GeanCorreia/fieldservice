using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface IUserCacheService
{
    Task <UserAuthenticationCacheModel?> GetUserByExternalIdAsync(
        string externalId,
        CancellationToken ct = default);
    Task SaveUserAsync(UserAuthenticationCacheModel user, CancellationToken ct = default);
    Task<UserAuthenticationCacheModel?> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task RemoveUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
}