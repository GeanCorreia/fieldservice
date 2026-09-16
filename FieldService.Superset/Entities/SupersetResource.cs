using System.Text.Json;
using FieldService.Shared.Types;
using FieldService.Superset.Dtos;
using FieldService.Superset.Enums;
using System.Text.RegularExpressions;

namespace FieldService.Superset.Entities;

internal class SupersetResource
{
    private const string _permissionModule =  "superset";
    private const string _readAccessAction = "read";
    private const string _writeAccessAction = "write";
    
    public Guid Id { get; init; }
    public string SupersetResourceId { get; init; }
    public string DisplayName { get; init; }
    public SupersetResourceType ResourceType { get; init; }
    private JsonElement _tenantIds = new();
    public IReadOnlyCollection<Guid> TenantIds => JsonSerializer.Deserialize<List<Guid>>(_tenantIds.GetRawText()) ?? new List<Guid>();
    
    private JsonElement _dataTarget = new();
    public SupersetDataTarget DataTarget => JsonSerializer.Deserialize<SupersetDataTarget>(_dataTarget.GetRawText()) 
                                           ?? throw new Exception("DataTarget could not be deserialized.");

    private JsonElement? _clientDatabaseTarget = new();
    public ClientDatabaseTarget? ClientDatabaseTarget => _clientDatabaseTarget.HasValue 
                                                           ? JsonSerializer.Deserialize<ClientDatabaseTarget>(_clientDatabaseTarget.Value.GetRawText()) 
                                                           : null;
    private JsonElement? _clientApiTarget = new();
    public ClientApiTarget? ClientApiTarget => _clientApiTarget.HasValue
                                                 ? JsonSerializer.Deserialize<ClientApiTarget>(_clientApiTarget.Value.GetRawText())
                                                 : null;
    
    private JsonElement? _fields = new();
    
    public IReadOnlyCollection<SupersetResourceField> Fields => _fields.HasValue
            ? JsonSerializer.Deserialize<List<SupersetResourceField>>(_fields.Value.GetRawText())
            : new List<SupersetResourceField>();
    
    public string? SupersetDescription { get; private set; }
    
    public string? DevelopmentNotes { get; private set; }
    
    protected SupersetResource() { }
    
    private SupersetResource(
        Guid id, 
        string supersetResourceId, 
        string displayName, 
        SupersetResourceType resourceType, 
        IReadOnlyCollection<Guid> tenantIds, 
        SupersetDataTarget dataTarget,
        ClientDatabaseTarget? clientDatabaseTarget = null,
        ClientApiTarget? clientApiTarget = null,
        IReadOnlyCollection<SupersetResourceField>? fields = null)
    {
        if(clientDatabaseTarget != null && clientApiTarget != null)
        {
            throw new ArgumentException("Cannot have both ClientDatabaseTarget and ClientApiTarget set at the same time.");
        }
        
        Id = id;
        SupersetResourceId = supersetResourceId;
        DisplayName = displayName;
        ResourceType = resourceType;
        _tenantIds = JsonSerializer.SerializeToElement(tenantIds);
        _dataTarget = JsonSerializer.SerializeToElement(dataTarget);
        _clientDatabaseTarget = clientDatabaseTarget != null ? JsonSerializer.SerializeToElement(clientDatabaseTarget) : null;
        _clientApiTarget = clientApiTarget != null ? JsonSerializer.SerializeToElement(clientApiTarget) : null;
        _fields = fields != null ? JsonSerializer.SerializeToElement(fields) : null;
    }

    public static SupersetResource Create(

        string supersetResourceId,
        string displayName,
        SupersetResourceType type,
        IReadOnlyCollection<Guid> tenantIds,
        SupersetDataTarget dataTarget,
        ClientDatabaseTarget? clientDatabaseTarget = null,
        ClientApiTarget? clientApiTarget = null,
        IReadOnlyCollection<SupersetResourceField>? fields = null,
        Guid? id = null)
    {
        return new SupersetResource(
            id ?? Guid.NewGuid(),
            supersetResourceId,
            displayName,
            type,
            tenantIds,
            dataTarget,
            clientDatabaseTarget,
            clientApiTarget,
            fields);
    }

    public bool HasReadAccess(UserTenantDto user)
    {
        if(!TenantIds.Contains(user.TenantDto.TenantId))
        {
            return false;
        }
        
        var permission = user.TenantDto.Permissions
            .FirstOrDefault(p => p.Module == _permissionModule &&
                                 p.Resource == SupersetResourceId && 
                                 p.Action == _readAccessAction);
        return permission != null;
                
    }

    public bool HasWriteAccess(UserTenantDto user)
    {
        if(!TenantIds.Contains(user.TenantDto.TenantId))
        {
            return false;
        }
        
        var permission = user.TenantDto.Permissions
            .FirstOrDefault(p => p.Module == _permissionModule &&
                                 p.Resource == SupersetResourceId && 
                                 p.Action == _writeAccessAction);
        return permission != null;
    }
    
    public void AddTenantAccess(Guid tenantId)
    {
        if (!TenantIds.Contains(tenantId))
        {
            var updatedTenantIds = TenantIds.ToList();
            updatedTenantIds.Add(tenantId);
            _tenantIds = JsonSerializer.SerializeToElement(updatedTenantIds);
        }
    }
    
    public void RemoveTenantAccess(Guid tenantId)
    {
        if (TenantIds.Contains(tenantId))
        {
            var updatedTenantIds = TenantIds.ToList();
            updatedTenantIds.Remove(tenantId);
            _tenantIds = JsonSerializer.SerializeToElement(updatedTenantIds);
        }
    }

    public void AddField(SupersetResourceField field)
    {
        var updatedFields = Fields.ToList();
        updatedFields.Add(field);
        _fields = JsonSerializer.SerializeToElement(updatedFields);
    }

    public void RemoveField(SupersetResourceField field)
    {
        var updatedFields = Fields.ToList();
        updatedFields.Remove(field);
        _fields = JsonSerializer.SerializeToElement(updatedFields);
    }
    
    public void UpdateSchema(IEnumerable<SupersetResourceField> newSchema)
    {
        _fields = JsonSerializer.SerializeToElement(newSchema);
    }
    
    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }
        
        var maxAllowedLength = ResourceType switch
        {
            SupersetResourceType.Chart     => 255,  
            SupersetResourceType.Dashboard => 500,  
            SupersetResourceType.Database  => 250,  
            SupersetResourceType.Dataset   => 1000, 
            _ => throw new ArgumentOutOfRangeException(nameof(ResourceType), $"Tipo de recurso não suportado: {ResourceType}")
        };
        
        var sanitizedDescription = SanitizeDescription(description);
        
        if (sanitizedDescription.Length > maxAllowedLength)
        {
            throw new ArgumentException(
                $"Description for {ResourceType} cannot exceed {maxAllowedLength} characters. (Sent: {sanitizedDescription.Length})", 
                nameof(description));
        }
       
        SupersetDescription = sanitizedDescription;
    }

    public void UpdateDevelopmentNotes(string developmentNotes)
    {
        if (string.IsNullOrWhiteSpace(developmentNotes))
        {
            return;
        }
        
        var maxAllowedLength = 1000; 
        
        var sanitizedNotes = SanitizeDescription(developmentNotes);
        
        if (sanitizedNotes.Length > maxAllowedLength)
        {
            throw new ArgumentException(
                $"Development notes cannot exceed {maxAllowedLength} characters. (Sent: {sanitizedNotes.Length})", 
                nameof(developmentNotes));
        }
       
        DevelopmentNotes = sanitizedNotes;
    }
    
    private string SanitizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;
        
        var noHtml = Regex.Replace(description, @"<[^>]*>", string.Empty);
        var singleSpaced = Regex.Replace(noHtml, @"\s+", " ");
        return singleSpaced.Trim();
    }
    
    public void UpdateDataTarget(SupersetDataTarget newDataTarget)
    {
        _dataTarget = JsonSerializer.SerializeToElement(newDataTarget);
    }

    public void UpdateClientDatabaseTarget(ClientDatabaseTarget newClientDatabaseTarget)
    {
        _clientDatabaseTarget = JsonSerializer.SerializeToElement(newClientDatabaseTarget);
        if(_clientDatabaseTarget != null)
        {
            _clientApiTarget = null;
        }
    }

    public void UpdateClientApiTarget(ClientApiTarget newClientApiTarget)
    {
        _clientApiTarget = JsonSerializer.SerializeToElement(newClientApiTarget);
        if(_clientApiTarget != null)
        {
            _clientDatabaseTarget = null;
        }
    }
    
        
}