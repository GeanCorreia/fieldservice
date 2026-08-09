using FieldService.Authorization.Entities;
using FieldService.Authorization.Types;

namespace FieldService.Authorization.Interfaces;

public interface IUserAuthorizationMapper
{
    UserAuthorizationSnapshot Map(UserAuthorizationContext user);
    
}