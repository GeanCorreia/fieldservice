namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

public sealed record BooleanFieldType : FieldTypeBase
{
    public static readonly BooleanFieldType Instance = new();

    private BooleanFieldType() { }

    public override string Name => "Boolean";

    public override IReadOnlySet<ValidatorType> AllowedRules => WithCommonRules();
}
