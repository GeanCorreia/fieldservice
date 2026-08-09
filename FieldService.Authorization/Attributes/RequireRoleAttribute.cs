using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(Role role)
    {
        Role = role;
        Policy = role.ToString();
    }

    public Role Role { get; }
}
