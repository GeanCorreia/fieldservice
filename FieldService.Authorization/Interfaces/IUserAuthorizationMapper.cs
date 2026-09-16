using FieldService.Authorization.Dtos;
using FieldService.Authorization.Entities;
using FieldService.Authorization.Types;

namespace FieldService.Authorization.Interfaces;

public interface IUserAuthorizationMapper
{
    UserAuthorizationDto Map(IEnumerable<UserAuthorization> userAuthorizations);
    
}