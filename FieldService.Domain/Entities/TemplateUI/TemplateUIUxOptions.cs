namespace FieldService.Domain.Entities;

public class TemplateUIUxOptions
{
    public static TemplateUIUxOptions Default => new();

    public TemplateUIUxOptions(
        bool useTabs = false,
        bool useStepper = false,
        bool showSummary = true,
        bool allowSectionCollapse = true)
    {
        UseTabs = useTabs;
        UseStepper = useStepper;
        ShowSummary = showSummary;
        AllowSectionCollapse = allowSectionCollapse;
    }

    public bool UseTabs { get; private set; }
    public bool UseStepper { get; private set; }
    public bool ShowSummary { get; private set; }
    public bool AllowSectionCollapse { get; private set; }

    public TemplateUIUxOptions Clone() => new(UseTabs, UseStepper, ShowSummary, AllowSectionCollapse);
}
