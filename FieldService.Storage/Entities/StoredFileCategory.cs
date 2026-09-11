using System.Net.Http.Headers;
using System.Text.Json;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Storage.Exceptions;
using FieldService.Storage.Interfaces;

namespace FieldService.Storage.Entities;

public class StoredFileCategory 
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Code { get; init; }
    public long? MaxSizeInBytes { get; init; }
    private JsonElement _allowedContentTypes { get; init; } = JsonDocument.Parse("[]").RootElement;
    public IReadOnlyList<MediaTypeHeaderValue> AllowedContentTypes =>
        _allowedContentTypes
            .EnumerateArray()
            .Select(x => MediaTypeHeaderValue.Parse(x.GetString()!))
            .ToList();
    
    public SchemaVersion Version { get; init; }
    
    public Role? MinimumRequiredRole { get; init; }
    private JsonElement _allowedPermissions { get; init; } = JsonDocument.Parse("[]").RootElement;
    public IReadOnlyList<Permission>? AllowedPermissions =>
        _allowedPermissions
            .EnumerateArray()
            .Select(x => JsonSerializer.Deserialize<Permission>(x.GetRawText())!)
            .ToList();
    
    public StoredFileCategory(
        Guid id,
        Guid tenantId,
        string code,
        long? maxSizeInBytes,
        IEnumerable<MediaTypeHeaderValue> allowedContentTypes,
        SchemaVersion version,
        Role? minimumRequiredRole,
        IEnumerable<Permission>? allowedPermissions)
    {
        Id = id;
        TenantId = tenantId;
        Code = code;
        MaxSizeInBytes = maxSizeInBytes;
        _allowedContentTypes = JsonDocument.Parse(JsonSerializer.Serialize(allowedContentTypes)).RootElement;
        Version = version;
        MinimumRequiredRole = minimumRequiredRole;
        _allowedPermissions = JsonDocument.Parse(JsonSerializer.Serialize(allowedPermissions)).RootElement;
    }
    
    private bool HasAccess(Role? userRole, IEnumerable<Permission>? userPermissions)
    {
        if (MinimumRequiredRole.HasValue && userRole < MinimumRequiredRole.Value)
        {
            return false;
        }

        if (AllowedPermissions != null && AllowedPermissions.Any())
        {
            if(userPermissions == null || !userPermissions.Any())
            {
                return false;
            }
            return AllowedPermissions.All(p => userPermissions.Contains(p));
        }

        return true;
    }
    
    public void ValidateAccess(UserAuthentication user)
    {
        var userRole = user.TenantDetails
            .FirstOrDefault(t => t.TenantId == TenantId)?.Role;
        
        var userPermissions = user.TenantDetails
            .FirstOrDefault(t => t.TenantId == TenantId)?.Permissions;
        
        if (!HasAccess(userRole, userPermissions))
        {
            throw new UnauthorizedAccessException("User does not have access to this stored file category.");
        }
    }
    
    public void ValidateMaxFileSize(long fileSize)
    {
        if (MaxSizeInBytes.HasValue && fileSize > MaxSizeInBytes.Value)
        {
            throw new FileSizeExceededException(fileSize, MaxSizeInBytes.Value);
        }
    }
    
    public void ValidateAllowedContentTypes(MediaTypeHeaderValue contentType)
    {
        if (AllowedContentTypes != null && AllowedContentTypes.Any())
        {
            if (!AllowedContentTypes.Any(x => x.MediaType.Equals(contentType.MediaType, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidContentTypeException(contentType.ToString(), AllowedContentTypes.Select(x => x.MediaType).ToList());
            }
        }
    }

}