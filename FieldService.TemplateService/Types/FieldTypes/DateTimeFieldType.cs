namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

public sealed record DateTimeFieldType : FieldTypeBase
{
    public static readonly DateTimeFieldType Instance = new();

    private DateTimeFieldType() { }

    public override string Name => "DateTime";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(ValidatorType.DateRange);
}
