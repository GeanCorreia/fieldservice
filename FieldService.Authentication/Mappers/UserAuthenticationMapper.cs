using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Mappers;

internal sealed class UserAuthenticationMapper : IUserAuthenticationMapper
{
    public UserAuthentication Map(UserAuthenticationCacheModel cacheModel)
    {
        ArgumentNullException.ThrowIfNull(cacheModel);

        return new UserAuthentication(
            cacheModel.UserId,
            cacheModel.ExternalId,
            cacheModel.Provider,
            cacheModel.Tenants.Select(t => new UserTenantAuthentication(
                t.UserId,
                t.TenantId,
                t.Name)).ToList());
    }

    public UserAuthenticationCacheModel Map(UserAuthentication userAuthentication)
    {
        ArgumentNullException.ThrowIfNull(userAuthentication);

        return new UserAuthenticationCacheModel(
            userAuthentication.UserId,
            userAuthentication.ExternalId,
            userAuthentication.Provider,
            userAuthentication.Tenants.Select(t => new UserTenantAuthenticationCacheModel(
                userAuthentication.UserId,
                t.TenantId,
                t.Name)).ToList());
    }
}
