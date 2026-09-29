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
        try
        {
            permission = Permission.Create(policyName);
            return true;
        }
        catch (ArgumentException)
        {
            permission = null!;
            return false;
        }
    }
}
