using System.Security.Claims;
using FieldService.Authorization.Dtos;
using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Logs;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace FieldService.Authorization.Services;

internal sealed class AuthorizationService : FieldService.Authorization.Interfaces.IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IUserContextRepository _userContextRepository;
    private readonly IUserAuthorizationMapper _authorizationMapper;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions;

    private const string UserContextPrefix = "authorization:user:";

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        IUserContextRepository userContextRepository,
        IUserAuthorizationMapper authorizationMapper,
        HybridCache hybridCache,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userContextRepository = userContextRepository ?? throw new ArgumentNullException(nameof(userContextRepository));
        _authorizationMapper = authorizationMapper ?? throw new ArgumentNullException(nameof(authorizationMapper));
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));

        var expirationSeconds = configuration.GetValue<int?>("Authorization:CacheExpirationInSeconds") ?? 86400;
        _cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(expirationSeconds)
        };
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
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        ct.ThrowIfCancellationRequested();

        return await _hybridCache.GetOrCreateAsync<UserAuthorizationDto?>(
            UserKey(userId),
            async token =>
            {
                var user = await _userContextRepository.GetUserAsync(userId, token);
                
                if (!user.Any())
                    return null;

                return _authorizationMapper.Map(user);
            },
            _cacheOptions,
            cancellationToken: ct);
    }

    private static string UserKey(Guid userId) => $"{UserContextPrefix}{userId:N}";
}