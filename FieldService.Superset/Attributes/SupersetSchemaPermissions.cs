using FieldService.Superset.Entities;

namespace FieldService.Superset.Attributes;

public static class SupersetSchemaPermissions
{
    public static string Developer => SupersetTenant.MetadataSchemaPrefix;
    public static string User => SupersetTenant.DataSchemaPrefix;
}