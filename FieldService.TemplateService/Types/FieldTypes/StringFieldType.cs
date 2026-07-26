namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

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
