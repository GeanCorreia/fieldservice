namespace FieldService.TemplateService.Entities;

public class TemplateUIEditability
{
    public static TemplateUIEditability Empty => new();

    public TemplateUIEditability(IEnumerable<TemplateUIConditionRule>? rules = null)
    {
        Rules = rules?.ToList() ?? new List<TemplateUIConditionRule>();
    }

    public IReadOnlyCollection<TemplateUIConditionRule> Rules { get; private set; }

    public TemplateUIEditability Clone() => new(Rules.Select(rule => rule.Clone()));
}
