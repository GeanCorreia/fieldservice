namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

public sealed record DateFieldType : FieldTypeBase
{
    public static readonly DateFieldType Instance = new();

    private DateFieldType() { }

    public override string Name => "Date";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(ValidatorType.DateRange);
}
