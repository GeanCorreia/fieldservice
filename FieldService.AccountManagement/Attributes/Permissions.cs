using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Attributes;

public static class Permissions
{
    public const string Read = "account-management:organization:read";
    public const string Edit = "account-management:organization:edit";

    public static readonly Permission ReadPermission = Permission.Create(Read);
    public static readonly Permission EditPermission = Permission.Create(Edit);
}
