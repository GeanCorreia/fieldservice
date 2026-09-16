using FieldService.Authentication.Dtos;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Interfaces;

public interface IUserAuthenticationMapper
{
    UserAuthenticationDto Map(UserAuthentication user);
} 