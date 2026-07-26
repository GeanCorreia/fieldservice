namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

public sealed record PhoneNumberFieldType : FieldTypeBase
{
    public static readonly PhoneNumberFieldType Instance = new();

    private PhoneNumberFieldType() { }

    public override string Name => "PhoneNumber";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(
            ValidatorType.Phone,
            ValidatorType.Regex);
}
