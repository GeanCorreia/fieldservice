using FieldService.Authentication.Dtos;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Mappers;

internal sealed class UserAuthenticationMapper : IUserAuthenticationMapper
{
    public UserAuthenticationDto Map(UserAuthentication user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var tenants = user.Tenants.Select(t => new TenantAuthenticationDto(t.TenantId, t.Name)).ToList();

        return new UserAuthenticationDto(user.UserId, tenants);
    }
}
