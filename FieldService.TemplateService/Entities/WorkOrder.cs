namespace FieldService.TemplateService.Entities;

public enum WorkOrderStatus
{
    Draft,
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public class WorkOrder
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public required string TenantId { get; set; }
    public required string TemplateId { get; set; }
    public required string Number { get; set; }
    public Dictionary<string, object> Data { get; private set; } = new();
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Description { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public WorkOrder() { }

    public WorkOrder(string tenantId, string templateId, string number)
    {
        TenantId = tenantId;
        TemplateId = templateId;
        Number = number;
    }

    public T GetFieldValue<T>(string fieldName)
    {
        if (Data.TryGetValue(fieldName, out var value))
            return (T)Convert.ChangeType(value, typeof(T)) ?? throw new InvalidOperationException($"Cannot convert field '{fieldName}' to type {typeof(T).Name}");

        throw new KeyNotFoundException($"Field '{fieldName}' not found in WorkOrder data.");
    }

    public void SetFieldValue<T>(string fieldName, T value)
    {
        Data[fieldName] = value ?? throw new ArgumentNullException(nameof(value));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool TryGetFieldValue<T>(string fieldName, out T? value) where T : class
    {
        value = default;
        if (Data.TryGetValue(fieldName, out var dataValue))
        {
            try
            {
                value = (T)Convert.ChangeType(dataValue, typeof(T))!;
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public void ApplyTemplateDefaults(TemplateVersion templateVersion)
    {
        foreach (var field in templateVersion.Fields.Where(f => f.DefaultValue != null && !Data.ContainsKey(f.Name)))
        {
            Data[field.Name] = field.DefaultValue!;
        }
    }
}