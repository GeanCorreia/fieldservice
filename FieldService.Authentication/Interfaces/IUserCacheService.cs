using FieldService.Authentication.Dtos;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Interfaces;

public interface IUserCacheService
{
    Task <UserAuthenticationDto?> GetUserByExternalIdAsync(
        string externalId,
        CancellationToken ct = default);
    Task SaveUserAsync(
        UserAuthenticationDto user, 
        string? externalId,
        CancellationToken ct = default);
    Task<UserAuthenticationDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task RemoveUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
}