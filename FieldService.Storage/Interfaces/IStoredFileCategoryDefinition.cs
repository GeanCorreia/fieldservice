using System.Net.Http.Headers;
using FieldService.Shared.Types;

namespace FieldService.Storage.Interfaces;

public interface IStoredFileCategoryDefinition
{

    public static string FileCode;
    public static long? FileMaxSizeInBytes;
    public static Guid FileTenantId;
    public static IEnumerable<MediaTypeHeaderValue> FileAllowedContentTypes;
    public static SchemaVersion FileVersion;
    public static Role? FileMinimumRequiredRole;
    public static IReadOnlyList<Permission> FileAllowedPermissions;
    
}