namespace FieldService.Domain.Entities;

public class TemplateUIPresentation
{
    public static TemplateUIPresentation Empty => new();

    public TemplateUIPresentation(
        string? title = null,
        string? subtitle = null,
        IEnumerable<TemplateUIFieldPresentation>? fields = null)
    {
        Title = title;
        Subtitle = subtitle;
        Fields = fields?.ToList() ?? new List<TemplateUIFieldPresentation>();
    }

    public string? Title { get; private set; }
    public string? Subtitle { get; private set; }
    public IReadOnlyCollection<TemplateUIFieldPresentation> Fields { get; private set; }

    public TemplateUIPresentation Clone() => new(Title, Subtitle, Fields.Select(field => field.Clone()));
}
