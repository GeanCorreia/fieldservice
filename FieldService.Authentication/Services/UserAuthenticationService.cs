using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Services;

public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly IUserAuthenticationRepository _userAuthenticationRepository;
    private readonly IUserCacheService _userCacheService;
    private readonly IUserAuthenticationMapper _userMapper;

    public UserAuthenticationService(
        IUserAuthenticationRepository userAuthenticationRepository,
        IUserCacheService userCacheService,
        IUserAuthenticationMapper userMapper)
    {
        _userAuthenticationRepository = userAuthenticationRepository ??
                                        throw new ArgumentNullException(nameof(userAuthenticationRepository));
        _userCacheService = userCacheService ?? throw new ArgumentNullException(nameof(userCacheService));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
    }

    public async Task<UserAuthentication?> GetUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var cacheModel = await _userCacheService.GetUserAsync(userId, cancellationToken);
        if (cacheModel != null)
        {
            return _userMapper.Map(cacheModel);
        }

        var user = await _userAuthenticationRepository.GetById(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }
        
        cacheModel = _userMapper.Map(user);
        await _userCacheService.SaveUserAsync(cacheModel, cancellationToken);
        return user;
    }

    public async Task<UserAuthenticationCacheModel?> GetUserAsync(
        string externalId, 
        AuthenticationProvider provider, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userCacheService.GetUserByExternalIdAsync(externalId, cancellationToken);
        if (user != null)
        {
            return user;
        }

        var userEntity = await _userAuthenticationRepository.GetByExternalId(
            externalId, 
            provider, 
            cancellationToken);
        
        if (userEntity == null)
        {
            return null;
        }

        var cacheModel = _userMapper.Map(userEntity);
        await _userCacheService.SaveUserAsync(cacheModel, cancellationToken);
        return cacheModel;
    }

    public async Task UpdateUserAsync(
        UserAuthentication userAuthentication, 
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(
            userAuthentication.UserId, 
            cancellationToken);

        if (user == null)
        {
            throw new NullReferenceException("User not found");
        }
        
        await _userAuthenticationRepository.Save(
            userAuthentication, 
            cancellationToken);
        
        await _userCacheService.RemoveUserAsync(
            userAuthentication.UserId, 
            cancellationToken);
        
        await _userCacheService.SaveUserAsync(
            _userMapper.Map(userAuthentication), 
            cancellationToken);
    }

}