
using FieldService.Shared.Types;

namespace FieldService.Superset.Attributes;

public enum SupersetRoleType
{

    Admin = 1,
    Alpha = 2,
    Gamma = 3,
    SqlLab = 4,
    DevelopmentScope = 5,
    ProductionScope = 6
    
}

public static class TenantScopeRole
{
    public static string RoleName(Guid tenantId) => $"Tenant_{tenantId:N}_Scope";
}

public static class SupersetRoleExtensions
{
    public static string ToRoleName(this SupersetRoleType roleType) => roleType switch
    {
        SupersetRoleType.Gamma => "Gamma",
        SupersetRoleType.Alpha => "Alpha",
        SupersetRoleType.Admin => "Admin",
        SupersetRoleType.SqlLab => "sql_lab",
        SupersetRoleType.DevelopmentScope => "development_scope",
        SupersetRoleType.ProductionScope => "production_scope",
        _ => throw new ArgumentOutOfRangeException(nameof(roleType), roleType, "Role do Superset desconhecida.")
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
    
    public static List<SupersetRoleType> MapToSupersetRoles(IEnumerable<Permission> permissions)
    {
        if (permissions == null || !permissions.Any())
        {
            return new List<SupersetRoleType> { SupersetRoleType.Gamma };
        }

        var roles = new HashSet<SupersetRoleType>();
        var policyNames = permissions.Select(p => p.PolicyName.ToLowerInvariant()).ToHashSet();
        
        if (policyNames.Contains(Admin))
        {
            roles.Add(SupersetRoleType.Admin);
        }
        else if (policyNames.Contains(Alpha))
        {
            roles.Add(SupersetRoleType.Alpha);
        }
        else
        {
            roles.Add(SupersetRoleType.Gamma);
        }
        
        if (policyNames.Contains(SqlLab))
        {
            roles.Add(SupersetRoleType.SqlLab);
        }

        if (policyNames.Contains(DevelopmentScope))
        {
            roles.Add(SupersetRoleType.DevelopmentScope);
        }

        if (policyNames.Contains(ProductionScope))
        {
            roles.Add(SupersetRoleType.ProductionScope);
        }

        return roles.ToList();
    }

    public static List<string> MapToSupersetRoleNames(IEnumerable<Permission> permissions)
    {
        return MapToSupersetRoles(permissions)
            .Select(role => role.ToRoleName())
            .ToList();
    }
    
    public static List<Permission> MapDomainPermissions(IEnumerable<SupersetRoleType> roles)
    {
        if (roles == null || !roles.Any())
        {
            return new List<Permission>();
        }

        var rolesSet = roles.ToHashSet();
        var permissions = new HashSet<Permission>();
        
        permissions.Add(AccessPermission);

        if (rolesSet.Contains(SupersetRoleType.Admin))
        {
            permissions.Add(AdminPermission);
        }
        else if (rolesSet.Contains(SupersetRoleType.Alpha))
        {
            permissions.Add(AlphaPermission);
        }
        else if (rolesSet.Contains(SupersetRoleType.Gamma))
        {
            permissions.Add(GammaPermission);
        }

        if (rolesSet.Contains(SupersetRoleType.SqlLab))
        {
            permissions.Add(SqlLabPermission);
        }

        if (rolesSet.Contains(SupersetRoleType.DevelopmentScope))
        {
            permissions.Add(DevelopmentScopePermission);
        }

        if (rolesSet.Contains(SupersetRoleType.ProductionScope))
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
            .OfType<SupersetRoleType>();

        return MapDomainPermissions(parsedRoles);
    }

    private static SupersetRoleType? TryParseRole(string roleName) => roleName.ToLowerInvariant() switch
    {
        "admin" => SupersetRoleType.Admin,
        "alpha" => SupersetRoleType.Alpha,
        "gamma" => SupersetRoleType.Gamma,
        "sql_lab" => SupersetRoleType.SqlLab,
        "development_scope" => SupersetRoleType.DevelopmentScope,
        "production_scope" => SupersetRoleType.ProductionScope,
        _ => null
    };
}
