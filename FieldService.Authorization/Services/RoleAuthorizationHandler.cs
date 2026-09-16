using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Requirements;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Services;

internal sealed class RoleAuthorizationHandler(
    FieldService.Authorization.Interfaces.IAuthorizationService authorizationService)
    : AuthorizationHandler<RoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var user = await authorizationService.GetUserAsync(context);
        if (user is null)
        {
            context.Fail();
            return;
        }
       
        var tenantId = ClaimsResolver.GetTenantId(context.User);
        
        var tenant = user.Tenants.FirstOrDefault(x => x.TenantId == tenantId);
        
        if (tenant is null)
        {
            context.Fail();
            return;
        }
        
        if (RoleRequirementValidator.SatisfiesRole(tenant.Role, requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }
        context.Fail();
    }

    
}
