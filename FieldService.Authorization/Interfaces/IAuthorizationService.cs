using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Interfaces;

public interface IAuthorizationService
{
    Task<UserDto?> GetUserAsync(
        AuthorizationHandlerContext context, 
        CancellationToken ct = default);
    Task<UserDto?> GetUserAsync(
        Guid userId, 
        CancellationToken ct = default);
}
