using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Requirements;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Services;

internal sealed class PermissionAuthorizationHandler(
    FieldService.Authorization.Interfaces.IAuthorizationService authorizationService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var snapshot = await authorizationService.GetSnapshotAsync(context);
        if (snapshot is not { IsActive: true })
            return;

        if (snapshot.Permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
