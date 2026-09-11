using FieldService.Authentication.Types;
using FieldService.Authorization.Types;
using FieldService.Http.Dtos;


namespace FieldService.Http.Interfaces;

public interface IUserMapper
{
    UserAuthenticationDto Map(
        UserAuthenticationCacheModel userAuthenticationCacheModel, 
        IEnumerable<UserAuthorizationSnapshot> userAuthorizationSnapshot);
}