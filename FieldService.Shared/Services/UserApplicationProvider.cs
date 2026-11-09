using FieldService.Shared.Configuration;
using FieldService.Shared.Types;
using Microsoft.Extensions.Options;

namespace FieldService.Shared.Services;

public class UserApplicationProvider
{
    private readonly ApplicationAccountOptions _options;
    public UserApplicationProvider(IOptions<ApplicationAccountOptions> options)
    {
        _options = options.Value;
    }
    public UserTenantDto GetApplicationUser()
    {
        var tenantDto = new UserAuthorizationTenantDto(
            TenantId: _options.TenantId,
            TenantName: _options.TenantName,
            Role: Role.Admin,
            Permissions: Array.Empty<Permission>(),
            IsActive: true
        );
        return new UserTenantDto(
            _options.ServiceUserId,
            tenantDto);

    }
    
    public bool IsApplicationUser(UserTenantDto user)
    {
        if (user == null) return false;
        if (user.TenantDto == null) return false;
        return user.Id == _options.ServiceUserId && user.TenantDto.TenantId == _options.TenantId;
    }
}