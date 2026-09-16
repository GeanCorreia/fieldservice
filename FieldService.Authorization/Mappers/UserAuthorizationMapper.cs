using FieldService.Authorization.Dtos;
using FieldService.Authorization.Entities;
using FieldService.Authorization.Interfaces;
using FieldService.Shared.Interfaces;

namespace FieldService.Authorization.Mappers;

public class UserAuthorizationMapper : IUserAuthorizationMapper
{
    private readonly IDateTimeService _dateTimeService;

    public UserAuthorizationMapper(IDateTimeService dateTimeService)
    {
        _dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
    }

    public UserAuthorizationDto Map(IEnumerable<UserAuthorization> userAuthorizations)
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
    
        var tenants = new List<TenantAuthorizationDto>();
    
        foreach (var userAuthorization in uniqueTenantsAuthorizations)
        {
            var tenant = new TenantAuthorizationDto(
                userAuthorization.TenantId,
                userAuthorization.Role,
                userAuthorization.Permissions.ToList(),
                userAuthorization.IsActive(now)
            );
            tenants.Add(tenant);
        }
    
        return new UserAuthorizationDto(
            userId,
            tenants,
            now
        );
    }
}