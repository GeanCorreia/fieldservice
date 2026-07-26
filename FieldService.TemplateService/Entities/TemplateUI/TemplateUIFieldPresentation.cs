namespace FieldService.TemplateService.Entities;

public class TemplateUIFieldPresentation
{
    public TemplateUIFieldPresentation(Guid fieldId, string? label = null, string? helperText = null, string? placeholder = null, string? icon = null)
    {
        FieldId = fieldId;
        Label = label;
        HelperText = helperText;
        Placeholder = placeholder;
        Icon = icon;
    }

    public Guid FieldId { get; private set; }
    public string? Label { get; private set; }
    public string? HelperText { get; private set; }
    public string? Placeholder { get; private set; }
    public string? Icon { get; private set; }

    public TemplateUIFieldPresentation Clone() => new(FieldId, Label, HelperText, Placeholder, Icon);
}
