using FieldService.Authentication.Types;
using FieldService.Authorization.Types;
using FieldService.Http.Dtos;
using FieldService.Http.Interfaces;
using FieldService.Shared.Dtos;
using TenantDetail = FieldService.Shared.Dtos.TenantDetail;

namespace FieldService.Http.Mappers;

public class UserMapper : IUserMapper
{
    public UserAuthenticationDto Map(
        UserAuthenticationCacheModel userAuthenticationCacheModel, 
        IEnumerable<UserAuthorizationSnapshot> userAuthorizationSnapshots)
    {
        ArgumentNullException.ThrowIfNull(userAuthenticationCacheModel);
        ArgumentNullException.ThrowIfNull(userAuthorizationSnapshots);

        var snapshotsList = userAuthorizationSnapshots.ToList();
        
        if (snapshotsList.Any(s => s.UserId != userAuthenticationCacheModel.UserId))
        {
            throw new ArgumentException(
                $"UserId mismatch: one or more snapshots do not belong to user '{userAuthenticationCacheModel.UserId}'.");
        }
        
        var snapshotsByTenantId = snapshotsList.ToDictionary(s => s.TenantId);
        
        var tenantDetails = userAuthenticationCacheModel.Tenants
            .Select(tenant =>
            {
                snapshotsByTenantId.TryGetValue(tenant.TenantId, out var snapshot);
                
                var permissionsAsStrings = snapshot?.Permissions != null
                    ? snapshot.Permissions.Select(p => p.ToString()).ToList()
                    : (IReadOnlyList<string>)Array.Empty<string>();

                return new TenantDetailDto(
                    TenantId: tenant.TenantId,
                    TenantName: tenant.Name,
                    
                    Role: snapshot?.Role.ToString() ?? string.Empty, 
                    
                    Permissions: permissionsAsStrings,
                    
                    IsActive: snapshot?.IsActive ?? false 
                );
            })
            .ToList();
        return new UserAuthenticationDto(
            tenantDetails
        );
    }
}