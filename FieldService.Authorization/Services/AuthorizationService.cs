using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Mappers;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace FieldService.Authorization.Services;

internal sealed class AuthorizationService : FieldService.Authorization.Interfaces.IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IUserContextRepository _userContextRepository;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions;

    private const string UserContextPrefix = "authorization:user:";

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        IUserContextRepository userContextRepository,
        HybridCache hybridCache,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userContextRepository = userContextRepository ?? throw new ArgumentNullException(nameof(userContextRepository));
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));

        var expirationSeconds = configuration.GetValue<int?>("Authorization:CacheExpirationInSeconds") ?? 86400;
        _cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(expirationSeconds)
        };
    }

    public async Task<UserDto?> GetUserAsync(
        AuthorizationHandlerContext context, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated != true)
            return null;

        var userId = ClaimsResolver.GetUserId(context.User);

        return await GetUserAsync(userId, ct);
    }

    public async Task<UserDto?> GetUserAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();

        var userKey = UserKey(userId);
        var userAuthorization = await _hybridCache.GetOrCreateAsync<UserDto?>(

            userKey,
            async token =>
            {
                var users = await _userContextRepository.GetUserAsync(userId, token);

                if (!users.Any())
                    return null;
                
                users = users.Where(u => u.IsRevoked(DateTimeOffset.UtcNow) == false).ToList();

                return UserAuthorizationMapper.Map(users);
            },
            _cacheOptions,
            cancellationToken: ct);

        if (userAuthorization is null)
        {
            await _hybridCache.RemoveAsync(userKey, ct);
        }

        return userAuthorization;
    }

    public async Task RemoveUserFromCacheAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();

        var userKey = UserKey(userId);
        await _hybridCache.RemoveAsync(userKey, ct);
    }

    private static string UserKey(Guid userId) => $"{UserContextPrefix}{userId:N}";
}