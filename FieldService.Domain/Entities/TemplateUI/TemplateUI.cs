namespace FieldService.Domain.Entities;

public class TemplateUI
{
    protected TemplateUI()
    {
        Layout = TemplateUILayout.Empty;
        Presentation = TemplateUIPresentation.Empty;
        Visibility = TemplateUIVisibility.Empty;
        Editability = TemplateUIEditability.Empty;
        DefaultValueStrategy = TemplateUIDefaultValueStrategy.Empty;
        UxOptions = TemplateUIUxOptions.Default;
    }

    public TemplateUI(
        TemplateUILayout? layout = null,
        TemplateUIPresentation? presentation = null,
        TemplateUIVisibility? visibility = null,
        TemplateUIEditability? editability = null,
        TemplateUIDefaultValueStrategy? defaultValueStrategy = null,
        TemplateUIUxOptions? uxOptions = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Layout = layout ?? TemplateUILayout.Empty;
        Presentation = presentation ?? TemplateUIPresentation.Empty;
        Visibility = visibility ?? TemplateUIVisibility.Empty;
        Editability = editability ?? TemplateUIEditability.Empty;
        DefaultValueStrategy = defaultValueStrategy ?? TemplateUIDefaultValueStrategy.Empty;
        UxOptions = uxOptions ?? TemplateUIUxOptions.Default;
    }

    public Guid Id { get; private set; }
    public TemplateUILayout Layout { get; private set; }
    public TemplateUIPresentation Presentation { get; private set; }
    public TemplateUIVisibility Visibility { get; private set; }
    public TemplateUIEditability Editability { get; private set; }
    public TemplateUIDefaultValueStrategy DefaultValueStrategy { get; private set; }
    public TemplateUIUxOptions UxOptions { get; private set; }

    public TemplateUI Clone()
    {
        return new TemplateUI(
            Layout.Clone(),
            Presentation.Clone(),
            Visibility.Clone(),
            Editability.Clone(),
            DefaultValueStrategy.Clone(),
            UxOptions.Clone(),
            Id);
    }
}
