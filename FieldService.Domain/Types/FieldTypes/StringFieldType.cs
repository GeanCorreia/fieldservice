namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;

public sealed record StringFieldType : FieldTypeBase
{
    public static readonly StringFieldType Instance = new();

    private StringFieldType() { }

    public override string Name => "String";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(
            ValidatorType.MinLength,
            ValidatorType.MaxLength,
            ValidatorType.Regex);
}
