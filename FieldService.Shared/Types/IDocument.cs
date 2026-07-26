namespace FieldService.Shared.Types;

public interface IDocument
{
    string Value { get; }
    string FormattedValue { get; }
    bool IsValid { get; }
}
