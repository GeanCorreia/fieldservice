using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Authentication.Services;

internal sealed class InternalUserAuthenticationService : IInternalUserAuthenticationService
{
    private readonly IUserAuthenticationRepository _userAuthenticationRepository;
    private readonly ILogger<InternalUserAuthenticationService> _logger;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions;

    private const string AuthenticationPrefix = "authentication:";
    private const string UserExternalPrefix = $"{AuthenticationPrefix}external:";

    public InternalUserAuthenticationService(
        IUserAuthenticationRepository userAuthenticationRepository,
        ILogger<InternalUserAuthenticationService> logger,
        HybridCache hybridCache,
        IOptions<AuthenticationOptions> options)
    {
        _userAuthenticationRepository = userAuthenticationRepository ?? throw new ArgumentNullException(nameof(userAuthenticationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var authOptions = options.Value;
        var ttl = TimeSpan.FromMinutes(authOptions.Session.TokenLifetimeInMinutes) +
                  TimeSpan.FromHours(authOptions.Session.CacheTtlExtraHours);

        _cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = ttl
        };
    }

    

    public async Task<Guid?> GetUserIdAsync(
        string sub,
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sub))
            throw new ArgumentException("ExternalId (sub) is required.", nameof(sub));

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedSub = sub.Trim();
        var externalKey = GetUserExternalKey(normalizedSub, provider);

        var userId = await _hybridCache.GetOrCreateAsync(
            externalKey,
            async token =>
            {
                _logger.LogDebug("Cache miss for authentication key {ExternalKey}. Loading from repository.", externalKey);

                var userAuthentication = await _userAuthenticationRepository.GetByExternalId(normalizedSub, provider, token);
                return userAuthentication?.UserId ?? Guid.Empty;
            },
            _cacheOptions,
            cancellationToken: cancellationToken);

        if (userId == Guid.Empty)
        {
            await _hybridCache.RemoveAsync(externalKey, cancellationToken);
            return null;
        }

        return userId;
    }
    
    private static string GetUserExternalKey(string externalId, AuthenticationProvider provider) =>
        $"{UserExternalPrefix}{provider:D}:{Uri.EscapeDataString(externalId)}";
    
}