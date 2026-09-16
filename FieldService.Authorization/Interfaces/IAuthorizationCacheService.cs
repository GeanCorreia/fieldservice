using FieldService.Authorization.Dtos;


namespace FieldService.Authorization.Interfaces;

public interface IAuthorizationCacheService
{
    Task<UserAuthorizationDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task SaveUserAsync(UserAuthorizationDto user, CancellationToken ct = default);
}