namespace FieldService.TemplateService.Entities;

public class TemplateUIDefaultValueStrategy
{
    public static TemplateUIDefaultValueStrategy Empty => new();

    public TemplateUIDefaultValueStrategy(IEnumerable<TemplateUIDefaultValueRule>? rules = null)
    {
        Rules = rules?.ToList() ?? new List<TemplateUIDefaultValueRule>();
    }

    public IReadOnlyCollection<TemplateUIDefaultValueRule> Rules { get; private set; }

    public TemplateUIDefaultValueStrategy Clone() => new(Rules.Select(rule => rule.Clone()));
}
