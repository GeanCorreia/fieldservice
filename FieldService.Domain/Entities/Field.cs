namespace FieldService.Domain.Entities;
using FieldService.Domain.Types.FieldTypes;
using FieldService.Domain.ValueObjects;
using FieldService.Shared.Utils;

public class Field
{
    public Guid Id { get; }
    public string Name { get; }
    private string? _label;
    public string Label
    {
        get => _label ?? Name;
        private set => _label = value;
    }
    public FieldTypeBase Type { get;}
    private ICollection<ValidationRule> _rules;
    public IReadOnlyCollection<ValidationRule> Rules => (IReadOnlyCollection<ValidationRule>)_rules;
    public object? DefaultValue { get; private set; }
    public int Order { get; private set; }
    public bool Visible { get; private set; } = true;
    public string? Placeholder { get; private set; }
    public string? Description { get; private set; }
    
    protected Field() { }
    
    public Field(
        Guid id,
        string name,
        FieldTypeBase type,
        int order,
        IEnumerable<ValidationRule> rules,
        string? label = null,
        bool isRequired = false,
        object? defaultValue = null,
        bool visible = true,
        string? placeholder = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Field name cannot be empty", nameof(name));
        }
        Id = id;
        Name = NormalizeTextService.Execute(name, nameof(name), "Field name cannot be empty.");
        Type = type;
        _label = string.IsNullOrWhiteSpace(label) ? null : NormalizeTextService.Execute(label, nameof(label), "Field label cannot be empty.");
        DefaultValue = defaultValue;
        Order = order;
        Visible = visible;
        Placeholder = string.IsNullOrWhiteSpace(placeholder) ? null : NormalizeTextService.Execute(placeholder, nameof(placeholder), "Field placeholder cannot be empty.");
        Description = string.IsNullOrWhiteSpace(description) ? null : NormalizeTextService.Execute(description, nameof(description), "Field description cannot be empty.");
        var parsedRules = new List<ValidationRule>(rules);
        if (isRequired && parsedRules.All(rule => rule.Type != Enums.ValidatorType.Required))
        {
            parsedRules.Add(ValidationRule.Required());
        }
        _rules = parsedRules;
    }

    
    public void SetLabel(string label)
    {
        _label = NormalizeTextService.Execute(label, nameof(label), "Field label cannot be empty.");
    }

    public void RemoveLabel()
    {
        _label = null;
    }
    
    public void SetDefaultValue(object? defaultValue)
    {
        DefaultValue = defaultValue;
    }

    internal void SetOrder(int order)
    {
        Order = order;
    }

    public void SetVisible(bool visible)
    {
        Visible = visible;
    }

    public void SetPlaceholder(string placeholder)
    {
        Placeholder = NormalizeTextService.Execute(placeholder, nameof(placeholder), "Field placeholder cannot be empty.");
    }

    public void RemovePlaceholder()
    {
        Placeholder = null;
    }

    public void SetDescription(string description)
    {
        Description = NormalizeTextService.Execute(description, nameof(description), "Field description cannot be empty.");
    }

    public void RemoveDescription()
    {
        Description = null;
    }
    
}
