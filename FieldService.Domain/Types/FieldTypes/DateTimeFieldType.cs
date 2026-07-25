namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;

public sealed record DateTimeFieldType : FieldTypeBase
{
    public static readonly DateTimeFieldType Instance = new();

    private DateTimeFieldType() { }

    public override string Name => "DateTime";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(ValidatorType.DateRange);
}
