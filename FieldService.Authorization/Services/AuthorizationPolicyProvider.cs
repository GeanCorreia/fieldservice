using FieldService.Authorization.Requirements;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FieldService.Authorization.Services;

internal sealed class FieldServiceAuthorizationPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (TryParsePermission(policyName, out var permission))
            return Task.FromResult<AuthorizationPolicy?>(BuildPolicy(new PermissionRequirement(permission)));

        if (Enum.TryParse<Role>(policyName, ignoreCase: true, out var role))
            return Task.FromResult<AuthorizationPolicy?>(BuildPolicy(new RoleRequirement(role)));

        return base.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy BuildPolicy(IAuthorizationRequirement requirement)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveUserRequirement(), requirement)
            .Build();
    }

    private static bool TryParsePermission(string policyName, out Permission permission)
    {
        permission = null!;

        var segments = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
            return false;

        var scope = segments[0].Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (scope.Length != 2)
            return false;

        permission = Permission.Create(scope[0], scope[1], segments[1]);
        return true;
    }
}
