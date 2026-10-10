using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FieldService.Form.Events;

namespace FieldService.Form.Entities;


internal record FormHistory(
    DateTimeOffset Position,
    Guid Id,
    Guid TenantId,
    bool IsActive,
    IEnumerable<FormSnapshot> Snapshots);

internal record FormSnapshot(
    string Slug,
    string Title,
    string? Description,
    JsonElement SchemaJson);

internal class Form
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    private List<FormEvent> _events = new List<FormEvent>();

    protected Form(){}
    private Form(
        Guid id, 
        Guid tenantId,
        List<FormEvent>? events = null)
    {
        Id = id;
        TenantId = tenantId;
        if (events != null)
        {
            _events = events;
        }
    }
    
    [NotMapped]
    public string Slug => Slugify(GetActualTitle());
    
    [NotMapped]
    public string Title => GetActualTitle();
    
    private string GetActualTitle(DateTimeOffset? at= null)
    {
        at ??= DateTimeOffset.UtcNow;
        var initialTitle = _events
            .FirstOrDefault(e => e.Type == FormEventType.Created)?.Title;

        if (string.IsNullOrWhiteSpace(initialTitle))
        {
            throw new InvalidOperationException("Form does not have a initial title.");
        }
        
        var lastUpdatedTitle = _events
            .Where(e => e.Type == FormEventType.Updated && 
                        !string.IsNullOrWhiteSpace(e.Title) &&
                        e.OccurredAt <= at.Value)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => e.Title)
            .FirstOrDefault();
        
        return lastUpdatedTitle ?? initialTitle;
        
    }
    
    [NotMapped]
    public bool IsActive => !_events.Any(e => e.Type == FormEventType.Deleted);
    
    [NotMapped]
    public string? Description => GetActualDescription();
    
    private string? GetActualDescription(DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        var initialDescription = _events
            .FirstOrDefault(e => e.Type == FormEventType.Created)?.Description;

        var lastUpdatedDescription = _events
            .Where(e => e.Type == FormEventType.Updated && 
                        !string.IsNullOrWhiteSpace(e.Description) 
                        && e.OccurredAt <= at.Value)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => e.Description)
            .FirstOrDefault();
        
        return lastUpdatedDescription ?? initialDescription;
    }
    
    [NotMapped]
    public JsonElement SchemaJson => GetActualSchemaJson();
    
    private JsonElement GetActualSchemaJson(DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        var initialSchemaJson = _events
            .FirstOrDefault(e => e.Type == FormEventType.Created)?.SchemaJson;

        if (initialSchemaJson == null)
        {
            throw new InvalidOperationException("Form does not have an initial schema.");
        }
        
        var lastUpdatedSchemaJson = _events
            .Where(e => e.Type == FormEventType.Updated && 
                        e.SchemaJson.HasValue && 
                        e.OccurredAt <= at.Value)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => e.SchemaJson)
            .FirstOrDefault();
        
        return lastUpdatedSchemaJson ?? initialSchemaJson.Value;
    }
    
    public FormHistory GetFormHistory()
    {
        var snapshots = new List<FormSnapshot>();

        var ocurrences = _events
            .Select(e => e.OccurredAt)
            .ToHashSet();

        foreach (var occurredAt in ocurrences.OrderBy(e => e))
        {
            var snapshot = new FormSnapshot(
                Slug: Slugify(GetActualTitle(occurredAt)),
                Title: GetActualTitle(occurredAt),
                Description: GetActualDescription(occurredAt),
                SchemaJson: GetActualSchemaJson(occurredAt));
            
            snapshots.Add(snapshot);
        }


        return new FormHistory(
            Position: DateTimeOffset.UtcNow,
            Id: Id,
            TenantId: TenantId,
            IsActive: IsActive,
            Snapshots: snapshots);
    }
    
    public static string Slugify(string title)
    {
        string slug = title.ToLowerInvariant();
        
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = slug.Trim('-');

        return slug;
    }

    public static string TitleCase(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title;
        
        var minorWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "de", "da", "do", "das", "dos", "a", "e", "o", "em", "para", "com", "por", "sem"
        };
        
        var words = title.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i].ToLower();
            
            if (i == 0 || !minorWords.Contains(word))
            {
                words[i] = char.ToUpper(word[0]) + word.Substring(1);
            }
            else
            {
                words[i] = word;
            }
        }

        return string.Join(" ", words);
    }
    
    public void UpdateForm(
        Guid userId,
        string? title = null, 
        string? description = null, 
        JsonElement? schemaJson = null)
    {
        if (title == null && description == null && schemaJson == null)
            return;

        var updateEvent = FormEvent.UpdateEvent(
            userId: userId,
            formId: Id,
            schemaJson: schemaJson,
            title: title,
            description: description);
        
        _events.Add(updateEvent);
    }
        
        
    public void DeleteForm(Guid userId)
    {
        var deleteEvent = FormEvent.DeleteEvent(
            userId: userId,
            formId: Id);
        
        _events.Add(deleteEvent);
    }

    public static Form Create(
        Guid tenantId,  
        Guid userId,
        JsonElement schemaJson,
        string title,
        string? description = null,
        Guid? id = null)
    {
        var form = new Form(
            id: id ?? Guid.NewGuid(),
            tenantId: tenantId);
        
        var createEvent = FormEvent.CreateEvent(
            userId: userId,
            formId: form.Id,
            schemaJson: schemaJson,
            title: title,
            description: description);
        
        form._events.Add(createEvent);
        
        return form;
    }
    
}