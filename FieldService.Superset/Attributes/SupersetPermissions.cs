
using FieldService.Shared.Types;

namespace FieldService.Superset.Attributes;

public enum SupersetRole
{

    Admin = 1,
    Alpha = 2,
    Gamma = 3,
    SqlLab = 4,
    DevelopmentScope = 5,
    ProductionScope = 6,
    TenantScope = 7
    
}

public static class TenantScopeRole
{
    public static string TenantScopeRoleName(Guid tenantId) => $"Tenant_{tenantId:N}_Scope";
}

public static class SupersetRoleExtensions
{
    public static string ToRoleName(this SupersetRole role) => role switch
    {
        SupersetRole.Gamma => "Gamma",
        SupersetRole.Alpha => "Alpha",
        SupersetRole.Admin => "Admin",
        SupersetRole.SqlLab => "sql_lab",
        SupersetRole.DevelopmentScope => "development_scope",
        SupersetRole.ProductionScope => "production_scope",
        SupersetRole.TenantScope => "tenant_scope",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Role do Superset desconhecida.")
    };
}

public static class SupersetPermissions
{

    public const string Access = "superset:workspace:access";
    public const string Gamma = "superset:workspace:gamma";
    public const string Alpha = "superset:workspace:alpha";
    public const string Admin = "superset:workspace:admin";
    public const string SqlLab = "superset:sqllab:execute";
    public const string DevelopmentScope = "superset:workspace:development";
    public const string ProductionScope = "superset:workspace:production";

    public static readonly Permission AccessPermission = Permission.Create(Access);
    public static readonly Permission GammaPermission = Permission.Create(Gamma);
    public static readonly Permission AlphaPermission = Permission.Create(Alpha);
    public static readonly Permission AdminPermission = Permission.Create(Admin);
    public static readonly Permission SqlLabPermission = Permission.Create(SqlLab);
    public static readonly Permission DevelopmentScopePermission = Permission.Create(DevelopmentScope);
    public static readonly Permission ProductionScopePermission = Permission.Create(ProductionScope);
    
    public static List<SupersetRole> MapToSupersetRoles(IEnumerable<Permission> permissions)
    {
        if (permissions == null || !permissions.Any())
        {
            return new List<SupersetRole> { SupersetRole.Gamma };
        }

        var roles = new HashSet<SupersetRole>();
        var policyNames = permissions.Select(p => p.PolicyName.ToLowerInvariant()).ToHashSet();
        
        if (policyNames.Contains(Admin))
        {
            roles.Add(SupersetRole.Admin);
        }
        else if (policyNames.Contains(Alpha))
        {
            roles.Add(SupersetRole.Alpha);
        }
        else
        {
            roles.Add(SupersetRole.Gamma);
        }
        
        if (policyNames.Contains(SqlLab))
        {
            roles.Add(SupersetRole.SqlLab);
        }

        if (policyNames.Contains(DevelopmentScope))
        {
            roles.Add(SupersetRole.DevelopmentScope);
        }

        if (policyNames.Contains(ProductionScope))
        {
            roles.Add(SupersetRole.ProductionScope);
        }

        return roles.ToList();
    }

    public static List<string> MapToSupersetRoleNames(IEnumerable<Permission> permissions)
    {
        return MapToSupersetRoles(permissions)
            .Select(role => role.ToRoleName())
            .ToList();
    }
    
    public static List<Permission> MapDomainPermissions(IEnumerable<SupersetRole> roles)
    {
        if (roles == null || !roles.Any())
        {
            return new List<Permission>();
        }

        var rolesSet = roles.ToHashSet();
        var permissions = new HashSet<Permission>();
        
        permissions.Add(AccessPermission);

        if (rolesSet.Contains(SupersetRole.Admin))
        {
            permissions.Add(AdminPermission);
        }
        else if (rolesSet.Contains(SupersetRole.Alpha))
        {
            permissions.Add(AlphaPermission);
        }
        else if (rolesSet.Contains(SupersetRole.Gamma))
        {
            permissions.Add(GammaPermission);
        }

        if (rolesSet.Contains(SupersetRole.SqlLab))
        {
            permissions.Add(SqlLabPermission);
        }

        if (rolesSet.Contains(SupersetRole.DevelopmentScope))
        {
            permissions.Add(DevelopmentScopePermission);
        }

        if (rolesSet.Contains(SupersetRole.ProductionScope))
        {
            permissions.Add(ProductionScopePermission);
        }

        return permissions.ToList();
    }
    
    public static List<Permission> MapDomainPermissions(IEnumerable<string> roleNames)
    {
        if (roleNames == null || !roleNames.Any())
        {
            return new List<Permission>();
        }

        var parsedRoles = roleNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(TryParseRole)
            .OfType<SupersetRole>();

        return MapDomainPermissions(parsedRoles);
    }

    private static SupersetRole? TryParseRole(string roleName) => roleName.ToLowerInvariant() switch
    {
        "admin" => SupersetRole.Admin,
        "alpha" => SupersetRole.Alpha,
        "gamma" => SupersetRole.Gamma,
        "sql_lab" => SupersetRole.SqlLab,
        "development_scope" => SupersetRole.DevelopmentScope,
        "production_scope" => SupersetRole.ProductionScope,
        _ => null
    };
}
