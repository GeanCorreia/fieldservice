using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Requirements;
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
        var snapshot = await authorizationService.GetSnapshotAsync(context);
        if (snapshot is not { IsActive: true })
            return;

        if (SatisfiesRole(snapshot.Role, requirement.Role))
            context.Succeed(requirement);
    }

    private static bool SatisfiesRole(Role userRole, Role requiredRole)
    {
        if (userRole == requiredRole)
            return true;

        return requiredRole switch
        {
            Role.Owner => false,
            Role.Admin => userRole is Role.Owner,
            Role.Supervisor => userRole is Role.Owner or Role.Admin,
            Role.Technician => userRole is Role.Owner or Role.Admin or Role.Supervisor,
            Role.Operator => userRole is Role.Owner or Role.Admin or Role.Supervisor,
            Role.Viewer => false,
            _ => false
        };
    }
}
