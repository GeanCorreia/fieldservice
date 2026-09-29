using FieldService.Shared.Types;
using Microsoft.AspNetCore.Authorization;

namespace FieldService.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(params string[] policyNames)
    {
        if (policyNames == null || policyNames.Length == 0)
            throw new ArgumentException("Pelo menos uma permissão deve ser informada.", nameof(policyNames));
        
        var validatedPermissions = policyNames
            .Select(Permission.Create)
            .Select(p => p.PolicyName);
        
        Policy = string.Join(",", validatedPermissions);
    }
}