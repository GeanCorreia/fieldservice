using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using FieldService.Storage.Exceptions;
using FiledService.Shared.Attributes;

namespace FieldService.Storage.Entities;

[Auditable(resource: nameof(StoredFileCategory), resourceIdPropertyName: nameof(StoredFileCategory.Id))]
public class StoredFileCategory 
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Code { get; init; }
    public long? MaxSizeInBytes { get; init; }
    [JsonInclude]
    private JsonElement _allowedContentTypes { get; init; } = JsonDocument.Parse("[]").RootElement;
    public IReadOnlyList<MediaTypeHeaderValue> AllowedContentTypes =>
        _allowedContentTypes
            .EnumerateArray()
            .Select(x => MediaTypeHeaderValue.Parse(x.GetString()!))
            .ToList();
    
    public SchemaVersion Version { get; init; }
    
    public Role? MinimumRequiredRole { get; init; }
    [JsonInclude]
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
        var contentTypeStrings = allowedContentTypes?.Select(x => x.MediaType ?? x.ToString()) ?? [];
        _allowedContentTypes = JsonDocument.Parse(JsonSerializer.Serialize(contentTypeStrings)).RootElement;
        Version = version;
        MinimumRequiredRole = minimumRequiredRole;
        _allowedPermissions = JsonDocument.Parse(JsonSerializer.Serialize(allowedPermissions)).RootElement;
    }

    [JsonConstructor]
    public StoredFileCategory(
        Guid id,
        Guid tenantId,
        string code,
        long? maxSizeInBytes,
        SchemaVersion version,
        Role? minimumRequiredRole,
        JsonElement _allowedContentTypes,
        JsonElement _allowedPermissions)
    {
        Id = id;
        TenantId = tenantId;
        Code = code;
        MaxSizeInBytes = maxSizeInBytes;
        Version = version;
        MinimumRequiredRole = minimumRequiredRole;
        this._allowedContentTypes = _allowedContentTypes;
        this._allowedPermissions = _allowedPermissions;
    }

    protected StoredFileCategory()
    {
    }
    
    private bool HasAccess(Role userRole, IEnumerable<Permission> userPermissions)
    {
        if( MinimumRequiredRole.HasValue && !RoleRequirementValidator.SatisfiesRole(userRole, MinimumRequiredRole.Value ))
        {
            return false;
        }

        if (AllowedPermissions.Any())
        {
            if(userPermissions == null || !userPermissions.Any())
            {
                return false;
            }
            return AllowedPermissions.All(p => userPermissions.Contains(p));
        }

        return true;
    }
    
    public void ValidateAccess(UserTenantDto userTenantDto)
    {
        ArgumentNullException.ThrowIfNull(userTenantDto);

        var tenantDetail = userTenantDto.TenantDto;
        var userRole = userTenantDto.TenantDto.Role;
        var userPermissions = userTenantDto.TenantDto.Permissions;
        
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