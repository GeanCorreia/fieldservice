using FieldService.Authorization.Entities;

namespace FieldService.Authorization.Interfaces;

public interface IUserContextRepository
{
    Task<IEnumerable<UserAuthorization>> GetUserAsync(
        Guid userId,
        CancellationToken ct = default);
    Task SaveUserContext(UserAuthorization user, CancellationToken ct = default);
    
}