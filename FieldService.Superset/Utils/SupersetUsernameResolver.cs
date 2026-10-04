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
    
    public static Guid ResolveDomainUserId(string supersetUsername)
    {
        if (string.IsNullOrWhiteSpace(supersetUsername))
            throw new ArgumentException("Superset username cannot be null or whitespace.", nameof(supersetUsername));

        var parts = supersetUsername.Split('_');
        if (parts.Length != 3 || parts[0] != "user")
            throw new ArgumentException($"Invalid Superset username format: {supersetUsername}", nameof(supersetUsername));

        if (!Guid.TryParse(parts[1], out var userId))
            throw new ArgumentException($"Invalid user ID in Superset username: {supersetUsername}", nameof(supersetUsername));

        return userId;
    }
}
