using FieldService.Shared.Types;

namespace FieldService.Superset.Utils;

public static class SupersetUsernameResolver
{
    public static string Resolve(UserTenantDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.TenantDto);

        var userId = user.Id.ToString();
        var tenantId = user.TenantDto.TenantId.ToString();

        return $"{userId}_{tenantId}";
    }
}