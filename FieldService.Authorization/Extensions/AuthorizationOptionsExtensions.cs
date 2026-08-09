using FieldService.Authorization.Requirements;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Extensions;

public static class AuthorizationOptionsExtensions
{
    public static AuthorizationOptions AddPermissionPolicy(
        this AuthorizationOptions options,
        string policyName,
        string module,
        string resource,
        string action)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        options.AddPolicy(policyName, builder => builder
            .RequireAuthenticatedUser()
            .AddRequirements(
                new ActiveUserRequirement(),
                new PermissionRequirement(Permission.Create(module, resource, action))));

        return options;
    }

    public static AuthorizationOptions AddRolePolicy(
        this AuthorizationOptions options,
        string policyName,
        Role role)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        options.AddPolicy(policyName, builder => builder
            .RequireAuthenticatedUser()
            .AddRequirements(
                new ActiveUserRequirement(),
                new RoleRequirement(role)));

        return options;
    }
}
