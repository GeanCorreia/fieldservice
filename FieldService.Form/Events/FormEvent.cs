using System.Text.Json;
using System.Text.Json.Nodes;

namespace FieldService.Form.Events;

internal enum FormEventType
{
    Created,
    Updated,
    Deleted
}

internal class FormEvent
{
    public Guid Id { get; init; }
    public FormEventType Type { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid UserId { get; init; }
    public Guid FormId { get; init; }
    public JsonElement? SchemaJson { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    
    protected FormEvent() { }
    
    public static string SubmissionIdPropertyName => "SubmissionId";
    
    private FormEvent(
        Guid id,
        FormEventType type,
        DateTimeOffset occurredAt,
        Guid userId,
        Guid formId,
        JsonElement? schemaJson = null,
        string? title = null,
        string? description = null)
    {
        Id = id;
        Type = type;
        OccurredAt = occurredAt;
        UserId = userId;
        FormId = formId;
        SchemaJson = schemaJson;
        Title = title;
        Description = description;
    }
    
    
    public static FormEvent CreateEvent(
        Guid userId,
        Guid formId,
        JsonElement? schemaJson = null,
        string? title = null,   
        string? description = null)
    {
        return new FormEvent(
            id: Guid.NewGuid(),
            type: FormEventType.Created,
            occurredAt: DateTimeOffset.UtcNow, 
            userId: userId,
            formId: formId,
            schemaJson: schemaJson,
            title: title,
            description: description);
    }
    
    public static FormEvent UpdateEvent(
        Guid userId,
        Guid formId,
        JsonElement? schemaJson = null,
        string? title = null,
        string? description = null)
    {
        
        if(schemaJson == null &&
           string.IsNullOrEmpty(title) &&
           string.IsNullOrEmpty(description))
        {
            throw new ArgumentException("At least one property must be provided for update.");
        }
        
        return new FormEvent(
            id: Guid.NewGuid(),
            type: FormEventType.Updated,
            occurredAt: DateTimeOffset.UtcNow, 
            userId: userId,
            formId: formId,
            schemaJson: schemaJson,
            title: title,
            description: description);
    }
    
    public static FormEvent DeleteEvent(
        Guid userId,
        Guid formId)
    {
        return new FormEvent(
            id: Guid.NewGuid(),
            type: FormEventType.Deleted,
            occurredAt: DateTimeOffset.UtcNow, 
            userId: userId,
            formId: formId);
    }

}

