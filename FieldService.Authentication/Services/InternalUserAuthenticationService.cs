using FieldService.Authentication.Dtos;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Logs;
using FieldService.Authentication.Types;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Authentication.Services;

internal sealed class InternalUserAuthenticationService : IInternalUserAuthenticationService
{
    private readonly IUserAuthenticationRepository _userAuthenticationRepository;
    private readonly IUserAuthenticationMapper _userMapper;
    private readonly ILogger<InternalUserAuthenticationService> _logger;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions;

    private const string AuthenticationPrefix = "authentication:";
    private const string UserPrefix = $"{AuthenticationPrefix}user:";
    private const string UserExternalPrefix = $"{AuthenticationPrefix}external:";

    public InternalUserAuthenticationService(
        IUserAuthenticationRepository userAuthenticationRepository,
        IUserAuthenticationMapper userMapper,
        ILogger<InternalUserAuthenticationService> logger,
        HybridCache hybridCache,
        IOptions<AuthenticationOptions> options)
    {
        _userAuthenticationRepository = userAuthenticationRepository ?? throw new ArgumentNullException(nameof(userAuthenticationRepository));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));

        var authOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        var ttl = TimeSpan.FromMinutes(authOptions.Session.TokenLifetimeInMinutes) +
                  TimeSpan.FromHours(authOptions.Session.CacheTtlExtraHours);

        _cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = ttl
        };
    }

    public async Task<UserAuthenticationDto?> GetUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        cancellationToken.ThrowIfCancellationRequested();

        return await _hybridCache.GetOrCreateAsync<UserAuthenticationDto?>(
            GetUserKey(userId),
            async token =>
            {
                var userAuthentication = await _userAuthenticationRepository.GetById(userId, token);
                if (userAuthentication == null)
                    return null;

                return _userMapper.Map(userAuthentication);
            },
            _cacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<UserAuthenticationDto?> GetUserAsync(
        string sub, 
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sub))
            throw new ArgumentException("ExternalId (sub) is required.", nameof(sub));

        cancellationToken.ThrowIfCancellationRequested();
        
        var userId = await _hybridCache.GetOrCreateAsync<Guid?>(
            GetUserExternalKey(sub),
            async token =>
            {
                var userAuthentication = await _userAuthenticationRepository.GetByExternalId(sub, provider, token);
                return userAuthentication?.UserId;
            },
            _cacheOptions,
            cancellationToken: cancellationToken);

        if (!userId.HasValue || userId.Value == default)
            return null;
        
        return await GetUserAsync(userId.Value, cancellationToken);
    }
    
    private static string GetUserKey(Guid userId) => $"{UserPrefix}{userId:N}";
    private static string GetUserExternalKey(string externalId) =>
        $"{UserExternalPrefix}{Uri.EscapeDataString(externalId.Trim())}";
    
}