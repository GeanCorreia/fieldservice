using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Events;
using FieldService.SecretKey.Interfaces;

namespace FieldService.SecretKey.Entities;

internal class SecretKey
{
    public Guid Id { get; init; }
    public SecretKeyType Type { get; init; }
    public Guid TenantId { get; init; }
    private List<SecretKeyEvent> _secretKeyEvents = new List<SecretKeyEvent>();
    public ICollection<SecretKeyEvent> SecretKeyEvents => _secretKeyEvents.AsReadOnly();
    
    public SecretKeyReferenceDto Reference => ToReferenceDto();
    
    public ICollection<SecretKeyHistoryDto> History => _secretKeyEvents
        .OrderBy(e => e.OccurredAt)
        .Select(e => new SecretKeyHistoryDto(
            AtTime: e.OccurredAt,
            Reference: ToReferenceDto(e.OccurredAt)
            ))
        .ToList();
    
    protected SecretKey() { }
    
    public SecretKeyReferenceDto SecretKeyAt(DateTimeOffset? atTime = null)
    {
        atTime ??= DateTimeOffset.UtcNow;
        return ToReferenceDto(atTime);
    }

    private SecretKeyReferenceDto ToReferenceDto(
        DateTimeOffset? atTime = null)
    {
        IEnumerable<SecretKeyEvent> secretKeyEvents = _secretKeyEvents;
        DateTimeOffset cutoffTime = atTime ?? DateTimeOffset.UtcNow;
        
        var lastEventName = secretKeyEvents 
            .Where(e => e.OccurredAt <= cutoffTime 
                        && (e.EventType == SecretKeyEventType.Created || e.EventType == SecretKeyEventType.UpdatedName) 
                        && !string.IsNullOrWhiteSpace(e.Name))
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => $"tenant-{TenantId:N}-{e.Name}")
            .FirstOrDefault() ?? throw new InvalidOperationException($"No name found for secret key '{Id}' at or before '{cutoffTime}'.");
        
        var lastEventTypeName = secretKeyEvents
            .Where(e => e.OccurredAt <= cutoffTime 
                        && (e.EventType == SecretKeyEventType.Created || e.EventType == SecretKeyEventType.UpdatedType) 
                        && !string.IsNullOrWhiteSpace(e.TypeName))
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => e.TypeName)
            .FirstOrDefault() ?? throw new InvalidOperationException($"No type name found for secret key '{Id}' at or before '{cutoffTime}'.");
        
        var isDeleted = IsDeletedAt(cutoffTime);
        
        return new SecretKeyReferenceDto(
            Id: Id,
            Type: Type,
            TenantId: TenantId,
            Name: lastEventName,
            TypeName: lastEventTypeName,
            IsDeleted: isDeleted
        );
    }
    
    public bool IsDeleted => IsDeletedAt();
    
    private bool IsDeletedAt(DateTimeOffset? atTime = null)
    {
        DateTimeOffset cutoffTime = atTime ?? DateTimeOffset.UtcNow;
        
        var lastEvent = _secretKeyEvents
            .Where(e => e.OccurredAt <= cutoffTime)
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefault();
        
        return lastEvent?.EventType == SecretKeyEventType.Deleted;
    }
    
    private SecretKey(
        Guid id, 
        Guid tenantId,
        SecretKeyType type,
        ICollection<SecretKeyEvent>? secretKeyEvents = null)
    {
        Id = id;
        TenantId = tenantId;
        Type = type;
        if (secretKeyEvents != null)
        {
            _secretKeyEvents = new List<SecretKeyEvent>(secretKeyEvents);
        }
    }

    private void Update(
        SecretKeyEventType eventType, 
        Guid userId,
        string? name = null,
        string? typeName = null)
    {
       
        var newEvent = SecretKeyEvent.Create(
            userId: userId,
            secretKeyId: Id,
            eventType: eventType,
            name: name,
            typeName: typeName
        );
        _secretKeyEvents.Add(newEvent);
    }
    
    public static SecretKey Create(
        ISecretKeyType secretKeyType,
        string name, 
        Guid tenantId, 
        Guid userId)
    {
        var typeName = secretKeyType.GetType().Name;
        
        var secretKey = new SecretKey(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            type: secretKeyType.Type    
        );
        
        secretKey.Update(
            SecretKeyEventType.Created, 
            userId,
            SanitizeName(name),
            typeName);
        
        return secretKey;
    }
    
    public void UpdateName(string newName, Guid userId)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException($"Cannot update name for secret key '{Id}' after deletion.");
        }
           
        Update(
            SecretKeyEventType.UpdatedName,
            userId,
            SanitizeName(newName));
    }

    public void UpdateType(Guid userId, SecretKeyType secretKeyType)
    {
        
        Update(
            SecretKeyEventType.UpdatedType,
            userId,
            typeName: secretKeyType.ToString());
    }
    
    public void Delete(Guid userId)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException($"Cannot delete secret key '{Id}' after deletion.");
        }
        
        Update(
            SecretKeyEventType.Deleted,
            userId);
    }
    
    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da chave não pode ser vazio.", nameof(name));
        
        
        
        string normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        string result = sb.ToString().Normalize(NormalizationForm.FormC);
        
        result = result.ToLowerInvariant().Trim();
        result = Regex.Replace(result, @"[\s_]+", "-");
        
        result = Regex.Replace(result, @"[^a-z0-9-]", "");
        
        result = Regex.Replace(result, @"-+", "-");
        
        result = result.Trim('-');
        
        if (result.Length > 127)
            result = result.Substring(0, 127).TrimEnd('-');

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException($"O nome '{name}' não possui caracteres válidos para o Key Vault.");
        
        if (result.Length > 87)
            result = result.Substring(0, 83).TrimEnd('-');
        
        return result;
    }
}

    