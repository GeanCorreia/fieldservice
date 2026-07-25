namespace FieldService.Domain.ValueObjects;

public interface IDocument
{
    string Value { get; }
    string FormattedValue { get; }
    bool IsValid { get; }
}
