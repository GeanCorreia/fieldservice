namespace FieldService.Domain.Entities;

public class TemplateUIDefaultValueRule
{
    public TemplateUIDefaultValueRule(Guid fieldId, string strategy, string? expression = null, object? constant = null)
    {
        if (string.IsNullOrWhiteSpace(strategy))
            throw new ArgumentException("Strategy cannot be empty.", nameof(strategy));

        FieldId = fieldId;
        Strategy = strategy.Trim();
        Expression = expression;
        Constant = constant;
    }

    public Guid FieldId { get; private set; }
    public string Strategy { get; private set; }
    public string? Expression { get; private set; }
    public object? Constant { get; private set; }

    public TemplateUIDefaultValueRule Clone() => new(FieldId, Strategy, Expression, Constant);
}
