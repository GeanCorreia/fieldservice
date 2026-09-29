using FieldService.Superset.Entities;

namespace FieldService.Superset.Attributes;

public static class SupersetSchemaPermissions
{
    public static string Developer => SupersetTenantConfig.MetadataSchemaPrefix;
    public static string User => SupersetTenantConfig.DataSchemaPrefix;
}