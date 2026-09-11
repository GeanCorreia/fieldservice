namespace FieldService.TemplateService.Entities;

using FieldService.TemplateService.Types;
using FieldService.Shared.Types;

public class TemplateVersion
{
    private readonly List<Field> _fields;

    public Guid Id { get; } = Guid.NewGuid();
    public string? Description { get; private set; }
    public IReadOnlyCollection<Field> Fields => _fields;
    public SchemaVersion SchemaVersion { get; private set; }
    public TemplateUI Ui { get; private set; }

    private ICollection<DateTimeInterval> _activeIntervals;
    public IReadOnlyCollection<DateTimeInterval> ActiveIntervals => (IReadOnlyCollection<DateTimeInterval>)_activeIntervals;
    public bool IsActive => ActiveIntervals.Any(interval => interval.IsActive());
    
    public TemplateVersion(
        SchemaVersion schemaVersion, 
        IEnumerable<Field> fields,
        IEnumerable<DateTimeInterval>activeIntervals,
        TemplateUI? ui = null,
        string? description = null)
    {
        Description = description;
        SchemaVersion = schemaVersion;
        _fields = new List<Field>(fields);
        _activeIntervals = new List<DateTimeInterval>(activeIntervals);
        Ui = ui ?? new TemplateUI();

    }
    public void Activate(DateTimeOffset activationDate)
    {
        if (IsActive)
            throw new InvalidOperationException($"Template version '{SchemaVersion}' is already active.");
        
        if (ActiveIntervals.Any(interval => interval.IsActive()))
            throw new InvalidOperationException(
                $"Template version '{SchemaVersion}' was previously active and cannot be reactivated. Create a new version instead.");
        
        _activeIntervals.Add(new DateTimeInterval(activationDate));
    }

    public void Deactivate(DateTimeOffset deactivationDate)
    {
        if (!IsActive)
            throw new InvalidOperationException($"Template version '{SchemaVersion}' is not active.");
        
        var activeInterval = ActiveIntervals.FirstOrDefault(interval => interval.IsActive());
        if (activeInterval == null)
            throw new InvalidOperationException($"Template version '{SchemaVersion}' has no active interval.");
        
        if (deactivationDate <= activeInterval.Start)
            throw new ArgumentException(
                $"Deactivation date must be after activation date ({activeInterval.Start:yyyy-MM-dd HH:mm:ss}).", 
                nameof(deactivationDate));
        
        activeInterval.EndedAt(deactivationDate);
    }
    
    public bool WasActiveAt(DateTimeOffset moment) => ActiveIntervals.Any(interval => interval.WasActiveAt(moment));
    
    public bool WasActiveDuring(DateTimeOffset periodStart, DateTimeOffset periodEnd) => 
        ActiveIntervals.Any(interval => interval.WasActiveDuring(periodStart, periodEnd));

    public void ChangeFieldOrder(Guid fieldId, int newOrder)
    {
        if (newOrder < 1)
            throw new ArgumentOutOfRangeException(nameof(newOrder), "Order must be greater than zero.");

        var orderedFields = _fields.OrderBy(field => field.Order).ToList();
        var targetField = orderedFields.FirstOrDefault(field => field.Id == fieldId);
        if (targetField == null)
            throw new KeyNotFoundException($"Field '{fieldId}' was not found in template version '{SchemaVersion}'.");

        orderedFields.Remove(targetField);
        var insertionIndex = Math.Min(newOrder - 1, orderedFields.Count);
        orderedFields.Insert(insertionIndex, targetField);

        for (var i = 0; i < orderedFields.Count; i++)
        {
            orderedFields[i].SetOrder(i + 1);
        }

        _fields.Clear();
        _fields.AddRange(orderedFields);
    }
    
    public static TemplateVersion Create(
        DateTimeOffset createdAt,
        IEnumerable<Field> fields,
        SchemaVersion schemaVersion,
        string? description = null,
        TemplateUI? ui = null)
    {
        if (schemaVersion == null)
            throw new ArgumentException("Version cannot be null.", nameof(schemaVersion));

        var activeInterval = new[]{ new DateTimeInterval(createdAt) };
    
        var fieldList = fields as IList<Field> ?? fields.ToList();
        if (fieldList.Count == 0)
            throw new ArgumentException("Fields cannot be empty.", nameof(fields));

        var templateVersion = new TemplateVersion(
            schemaVersion,
            fieldList,
            activeInterval,
            ui: ui,
            description: description);
        
        return templateVersion;
    }
}
