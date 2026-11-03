using FieldService.Authorization.Entities;
using FieldService.Shared.Types;

namespace FieldService.Authorization.Mappers;

public static class UserAuthorizationMapper
{


   

    public static UserDto Map(IEnumerable<UserAuthorization> userAuthorizations)
    {
        var now = DateTimeOffset.UtcNow;
        
        var authorizationsList = userAuthorizations?.ToList() ?? new List<UserAuthorization>();
    
        if (!authorizationsList.Any())
        {
            throw new InvalidOperationException("User authorization list cannot be empty.");
        }
        
        if (authorizationsList.Select(u => u.UserId).Distinct().Count() > 1)
        {
            throw new InvalidOperationException("All authorizations must belong to the same user.");
        }
    
        var userId = authorizationsList.First().UserId;
        
        var uniqueTenantsAuthorizations = authorizationsList.DistinctBy(u => u.TenantId);
    
        var tenants = new List<UserAuthorizationTenantDto>();
    
        foreach (var userAuthorization in uniqueTenantsAuthorizations)
        {
            var tenant = new UserAuthorizationTenantDto(
                userAuthorization.TenantId,
                string.Empty,
                userAuthorization.Role,
                userAuthorization.Permissions.ToList(),
                userAuthorization.IsActive(now)
            );
            tenants.Add(tenant);
        }
    
        return new UserDto(
            userId,
            tenants
        );
    }
}