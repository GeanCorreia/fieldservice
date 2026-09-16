using FieldService.Authentication.Dtos;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Logs;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;
using Microsoft.Extensions.Logging;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Authentication.Services;

internal class InternalUserAuthenticationService : IInternalUserAuthenticationService
{
    private readonly IUserAuthenticationRepository _userAuthenticationRepository;
    private readonly IUserCacheService _userCacheService;
    private readonly IUserAuthenticationMapper _userMapper;
    private readonly ILogger<InternalUserAuthenticationService> _logger;

    public InternalUserAuthenticationService(
        IUserAuthenticationRepository userAuthenticationRepository,
        IUserCacheService userCacheService,
        IUserAuthenticationMapper userMapper,
        ILogger<InternalUserAuthenticationService> logger)
    {
        _userAuthenticationRepository = userAuthenticationRepository ??
                                        throw new ArgumentNullException(nameof(userAuthenticationRepository));
        _userCacheService = userCacheService ?? throw new ArgumentNullException(nameof(userCacheService));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    private async Task CreateCache(
        UserAuthentication userAuthentication,
        CancellationToken cancellationToken = default,
        string? sub = null)
    {
        try
        {
            var userAuthenticationTenants = _userMapper.Map(userAuthentication);
            await _userCacheService.SaveUserAsync(userAuthenticationTenants, sub, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogUserAuthorizationCacheError(
                LogLevel.Error,
                userAuthentication.UserId,
                ex);
        }
    }


    public async Task<UserAuthenticationDto?> GetUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var userDto = await _userCacheService.GetUserByIdAsync(userId, cancellationToken);
        if (userDto != null)
        {
            return userDto;
        }
        
        var userAuthentication = await _userAuthenticationRepository.GetById(userId, cancellationToken);
        if (userAuthentication == null)
        {
            return null;
        }
        
        await CreateCache(userAuthentication, cancellationToken);
        return _userMapper.Map(userAuthentication);
    }

    public async Task<UserAuthenticationDto?> GetUserAsync(
        string sub, 
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default)
    {
        var userDto = await _userCacheService.GetUserByExternalIdAsync(sub, cancellationToken);
        if (userDto != null)
        {
            return userDto;
        }
        
        var userAuthentication = await _userAuthenticationRepository.GetByExternalId(
            sub, 
            provider, 
            cancellationToken);
        
        if (userAuthentication == null)
        {
            return null;
        }
        
        await CreateCache(userAuthentication, cancellationToken, sub);
        return _userMapper.Map(userAuthentication);
    }

   
}