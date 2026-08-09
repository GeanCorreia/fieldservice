using FieldService.Authorization.Types;

namespace FieldService.Authorization.Interfaces;

public interface IAuthorizationCacheService
{
    Task<UserAuthorizationSnapshot?> GetUserContext(Guid userId, Guid tenantId,  CancellationToken ct = default);
    Task SaveUserContext(UserAuthorizationSnapshot userContext, CancellationToken ct = default);
}