using System.Security.Claims;
using FieldService.Authorization.Dtos;
using FieldService.Authorization.Entities;
using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Logs;
using FieldService.Authorization.Types;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace FieldService.Authorization.Services;

internal sealed class AuthorizationService(
    ILogger<AuthorizationService> logger,
    IUserContextRepository userContextRepository,
    IAuthorizationCacheService authorizationCacheService,
    IUserAuthorizationMapper authorizationMapper) : FieldService.Authorization.Interfaces.IAuthorizationService
{
    private async Task CreateCacheAsync(UserAuthorizationDto user,
        CancellationToken cancellationToken = default)
    {
        try
        {
           
            await authorizationCacheService.SaveUserAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogAuthorizationCache(
                LogLevel.Error,
                ex, 
                ex.Message);
        }
    }

    public async Task<UserAuthorizationDto?> GetUserAsync(
        AuthorizationHandlerContext context, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated != true)
            return null;

        var userId = ClaimsResolver.GetUserId(context.User);

        return await GetUserAsync(userId, ct);
    }

    public async Task<UserAuthorizationDto?> GetUserAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cached = await authorizationCacheService.GetUserByIdAsync(userId,  ct);
        if (cached is not null)
            return cached;

        var user = await userContextRepository.GetUserAsync(userId, ct);
        
        if (!user.Any())
            return null;
        
        var userDto = authorizationMapper.Map(user);
        await CreateCacheAsync(userDto, ct);
        return userDto;
    }
}
