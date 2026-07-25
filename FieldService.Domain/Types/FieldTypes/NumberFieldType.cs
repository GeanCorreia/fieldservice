namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;

public sealed record NumberFieldType : FieldTypeBase
{
    public static readonly NumberFieldType Instance = new();

    private NumberFieldType() { }

    public override string Name => "Number";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(
            ValidatorType.Min,
            ValidatorType.Max,
            ValidatorType.Decimal);
}
