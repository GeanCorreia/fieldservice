using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface IUserAuthenticationMapper
{
    UserAuthentication Map(UserAuthenticationCacheModel cacheModel);
    UserAuthenticationCacheModel Map(UserAuthentication userAuthentication);
}