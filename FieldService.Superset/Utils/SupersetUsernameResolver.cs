using FieldService.Shared.Types;

namespace FieldService.Superset.Utils;

public static class SupersetUsernameResolver
{
    public static string ResolveUsername(UserTenantDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.TenantDto);

        var userId = user.Id.ToString("N");
        var tenantId = user.TenantDto.TenantId.ToString("N");

        return $"user_{userId}_{tenantId}";
    }
    
    public static string ResolveUsername(Guid userId, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(tenantId);

        var userIdString = userId.ToString("N");
        var tenantIdString = tenantId.ToString("N");

        return $"user_{userIdString}_{tenantIdString}";
    }
    
    public static string ResolvePassword(UserTenantDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.TenantDto);

        var userId = user.Id.ToString("N");
        var tenantId = user.TenantDto.TenantId.ToString("N");

        return $"password_{userId}_{tenantId}";
    }
    
    public static string ResolvePassword(Guid userId, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(tenantId);

        var userIdString = userId.ToString("N");    
        var tenantIdString = tenantId.ToString("N");

        return $"password_{userIdString}_{tenantIdString}";
    }
}
