namespace FieldService.Domain.Entities;

public class TemplateUISection
{
    public TemplateUISection(string key, string title, IEnumerable<Guid>? fieldIds = null, bool collapsedByDefault = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Section key cannot be empty.", nameof(key));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Section title cannot be empty.", nameof(title));

        Key = key.Trim();
        Title = title.Trim();
        FieldIds = fieldIds?.ToList() ?? new List<Guid>();
        CollapsedByDefault = collapsedByDefault;
    }

    public string Key { get; private set; }
    public string Title { get; private set; }
    public IReadOnlyCollection<Guid> FieldIds { get; private set; }
    public bool CollapsedByDefault { get; private set; }

    public TemplateUISection Clone() => new(Key, Title, FieldIds, CollapsedByDefault);
}
