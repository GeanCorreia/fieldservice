namespace FieldService.TemplateService.Entities;

public class TemplateUIConditionRule
{
    public TemplateUIConditionRule(Guid targetFieldId, Guid sourceFieldId, string @operator, string value)
    {
        if (string.IsNullOrWhiteSpace(@operator))
            throw new ArgumentException("Operator cannot be empty.", nameof(@operator));

        TargetFieldId = targetFieldId;
        SourceFieldId = sourceFieldId;
        Operator = @operator.Trim();
        Value = value ?? string.Empty;
    }

    public Guid TargetFieldId { get; private set; }
    public Guid SourceFieldId { get; private set; }
    public string Operator { get; private set; }
    public string Value { get; private set; }

    public TemplateUIConditionRule Clone() => new(TargetFieldId, SourceFieldId, Operator, Value);
}
