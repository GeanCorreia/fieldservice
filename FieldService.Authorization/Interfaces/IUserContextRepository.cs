using FieldService.Authorization.Entities;

namespace FieldService.Authorization.Interfaces;

public interface IUserContextRepository
{
    Task<UserAuthorizationContext?> GetUserContext(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task SaveUserContext(UserAuthorizationContext userContext, CancellationToken ct = default);
    
}