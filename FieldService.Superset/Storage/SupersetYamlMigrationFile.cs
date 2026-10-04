using System.Net.Http.Headers;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;

namespace FieldService.Superset.Storage;

public class SupersetYamlMigrationFile : StoredFileCategory, IStoredFileCategoryDefinition
{
    public SupersetYamlMigrationFile() : 
        base(
            CategoryId, 
            FileTenantId, 
            FileCode, 
            FileMaxSizeInBytes, 
            FileAllowedContentTypes, 
            FileVersion, 
            FileMinimumRequiredRole, 
            FileAllowedPermissions)
    {
    }

    public static Guid CategoryId => Guid.Parse("270123f7-48c9-48f3-99cc-d974fa1c41a5");
    public static string FileCode => "SupersetYamlMigrationFile";
    public static long FileMaxSizeInBytes => 10485760; 
    public static Guid FileTenantId => Guid.Parse("48b2053d-09a1-4785-8f5e-a28f1d8087e4");
    public static IEnumerable<MediaTypeHeaderValue> FileAllowedContentTypes =>
        
        new List<MediaTypeHeaderValue>
        {
            new MediaTypeHeaderValue("application/x-yaml"),
            new MediaTypeHeaderValue("text/yaml"),
            new MediaTypeHeaderValue("application/yaml"),
            new MediaTypeHeaderValue("application/zip")
        };
    
    public static SchemaVersion FileVersion => new SchemaVersion(1, 0, 0);
    
    public static Role? FileMinimumRequiredRole => Role.Admin;
    
    public static IReadOnlyList<Permission> FileAllowedPermissions => Enumerable.Empty<Permission>().ToList();
    
}