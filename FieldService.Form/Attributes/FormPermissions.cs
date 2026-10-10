using FieldService.Shared.Types;

namespace FieldService.FormIO.Attributes;

public static class FormPermissions
{
    public const string SubmissionEdit = "form:form-submission:edit";
    public const string SubmissionDelete = "form:form-submission:delete";
    public const string SubmissionCreate = "form:form-submission:create";
    public const string SubmissionView = "form:form-submission:view";
    public const string SubmissionUnDelete = "form:form-submission:undelete";
    
    public const string Edit = "form:form:edit";
    public const string Delete = "form:form:delete";
    public const string Create = "form:form:create";
    public const string View = "form:form:view";
    public const string UnDelete = "form:form:undelete";
    
    
    public static readonly List<Permission> SubmissionPermissions = new()
    {
        SubmissionEditPermission,
        SubmissionDeletePermission,
        SubmissionCreatePermission,
        SubmissionViewPermission,
        SubmissionUnDeletePermission
    };

    public static readonly List<Permission> PermissionsList = new()
    {
        EditPermission,
        DeletePermission,
        CreatePermission,
        ViewPermission,
        UnDeletePermission
    };
    
    public static readonly List<Permission> AllPermissions = 
    [
        ..SubmissionPermissions,
        ..PermissionsList
    ];
    
    public static readonly Permission SubmissionEditPermission = Permission.Create(SubmissionEdit);
    public static readonly Permission SubmissionDeletePermission = Permission.Create(SubmissionDelete);
    public static readonly Permission SubmissionCreatePermission = Permission.Create(SubmissionCreate);
    public static readonly Permission SubmissionViewPermission = Permission.Create(SubmissionView); 
    public static readonly Permission SubmissionUnDeletePermission = Permission.Create(SubmissionUnDelete);
    
    public static readonly Permission EditPermission = Permission.Create(Edit);
    public static readonly Permission DeletePermission = Permission.Create(Delete);
    public static readonly Permission CreatePermission = Permission.Create(Create);
    public static readonly Permission ViewPermission = Permission.Create(View);
    public static readonly Permission UnDeletePermission = Permission.Create(UnDelete);
}