using FieldService.Shared.Types;

namespace FieldService.Shared.Services;

public static class RoleRequirementValidator
{
    public static bool SatisfiesRole(Role userRole, Role requiredRole)
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