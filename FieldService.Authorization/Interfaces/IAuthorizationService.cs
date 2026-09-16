using FieldService.Authorization.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Interfaces;

public interface IAuthorizationService
{
    Task<UserAuthorizationDto?> GetUserAsync(
        AuthorizationHandlerContext context, 
        CancellationToken ct = default);
    Task<UserAuthorizationDto?> GetUserAsync(
        Guid userId, 
        CancellationToken ct = default);
}
