namespace FieldService.Domain.Entities;

public class TemplateUIVisibility
{
    public static TemplateUIVisibility Empty => new();

    public TemplateUIVisibility(IEnumerable<TemplateUIConditionRule>? rules = null)
    {
        Rules = rules?.ToList() ?? new List<TemplateUIConditionRule>();
    }

    public IReadOnlyCollection<TemplateUIConditionRule> Rules { get; private set; }

    public TemplateUIVisibility Clone() => new(Rules.Select(rule => rule.Clone()));
}
