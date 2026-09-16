using System.Net.Http.Headers;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.Upload;

public class TestStoredFileCategory : StoredFileCategory, IStoredFileCategoryDefinition
{
    public TestStoredFileCategory() : 
        base(CategoryId, FileTenantId, FileCode, FileMaxSizeInBytes, FileAllowedContentTypes, FileVersion, FileMinimumRequiredRole, FileAllowedPermissions)
    {
    }

    public static Guid CategoryId => Guid.Parse("d3f1c8e2-5b6a-4f9e-9c1e-2b5f8e3d4a1b");
    
    public static string FileCode => "TestFileCategory";
    public static long FileMaxSizeInBytes => 5242880;
    public static Guid FileTenantId => Guid.Parse("4af9166c-15f7-4a42-acad-1042e3f8acef");
    public static IEnumerable<MediaTypeHeaderValue> FileAllowedContentTypes =>
        new List<MediaTypeHeaderValue>
        {
            new MediaTypeHeaderValue("application/pdf"),
            new MediaTypeHeaderValue("image/jpeg"),
            new MediaTypeHeaderValue("image/png")
        };
    
    public static SchemaVersion FileVersion => new SchemaVersion(1, 0, 0);
    
    public static Role? FileMinimumRequiredRole => Role.Admin;
    
    public static IReadOnlyList<Permission> FileAllowedPermissions => Enumerable.Empty<Permission>().ToList();
   
}
