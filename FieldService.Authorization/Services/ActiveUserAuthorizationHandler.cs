using FieldService.Authorization.Interfaces;
using FieldService.Authorization.Requirements;
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
        var snapshot = await authorizationService.GetSnapshotAsync(context);
        if (snapshot is { IsActive: true })
            context.Succeed(requirement);
    }
}
