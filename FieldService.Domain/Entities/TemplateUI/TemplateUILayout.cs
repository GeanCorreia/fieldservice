namespace FieldService.Domain.Entities;

public class TemplateUILayout
{
    public static TemplateUILayout Empty => new();

    public TemplateUILayout(IEnumerable<TemplateUISection>? sections = null, int columns = 1)
    {
        if (columns < 1)
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than zero.");

        Columns = columns;
        Sections = sections?.ToList() ?? new List<TemplateUISection>();
    }

    public int Columns { get; private set; }
    public IReadOnlyCollection<TemplateUISection> Sections { get; private set; }

    public TemplateUILayout Clone() => new(Sections.Select(section => section.Clone()), Columns);
}
