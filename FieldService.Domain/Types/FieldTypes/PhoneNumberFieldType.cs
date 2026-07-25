namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;

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
