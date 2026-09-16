using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Requirements;
using FieldService.Shared.Services;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Services;

internal sealed class ActiveUserAuthorizationHandler(
    FieldService.Authorization.Interfaces.IAuthorizationService authorizationService)
    : AuthorizationHandler<ActiveUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
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
        
        if (tenant.IsActive)
        {
            context.Succeed(requirement);
            return;
        }
        
        context.Fail();
    }
}
